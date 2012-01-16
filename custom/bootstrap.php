<?php
class Bootstrap
{
	private	$args, $root_dir, $root_web, $libraries, $modules, $themes, $templates, $styles, $scripts;
	
	private static $messages;
	
	private $html = array(
		'styles'	=> '<link type="text/css" rel="stylesheet" href="%s" media="%s" />',
		'scripts'	=> '<script type="text/javascript" src="%s"></script>',
	);
	
	private $autoload_libraries = array(
		// Autoload these JS libraries
		'jquery/jquery-1.4.2.min.js',
		'jquery/jquery.event.drag-2.0.min.js',
		'jquery/jquery.event.drop-2.0.min.js',
	);
	
	private static $autoload_modules_1 = array(
		// Autoload these modules' blocks for all pages in all modules
		// Each mod listed here also becomes a dependency for all other modules
		'pages',
	);
	
	
	
	/*
	 * Constructor
	 * 
	 * */
	public function __construct()
	{
		// Set global error handler
		set_error_handler( array('Bootstrap', 'handle_error'), E_STRICT );
		
		// Set path args
		$path = ltrim($_SERVER['REQUEST_URI'], '/');
		$this->args['path']			= (!empty($path) ? $path : '<front>');
		$this->args['path_parts']	= (!empty($path) ? explode('/', $path) : array());
		
		// Set working roots
		$this->root_dir	= dirname(__FILE__);
		$this->root_web	= explode(DIRECTORY_SEPARATOR, $this->root_dir);
		$this->root_web	= '/'.$this->root_web[count($this->root_web)-1];
		
		// Get installed modules + themes
		$this->libraries	= self::_get_info( $this->root_dir.DIRECTORY_SEPARATOR.'libraries' );
		$this->modules		= self::_get_info( $this->root_dir.DIRECTORY_SEPARATOR.'modules' );
		$this->themes		= self::_get_info( $this->root_dir.DIRECTORY_SEPARATOR.'themes' );
		$this->templates	= 
		$this->styles		= 
		$this->scripts		= array();
		
		
		// Autoload JS libraries
		for ($i=0; $i < count($this->autoload_libraries); $i++) {
			$this->scripts[] = vsprintf($this->html['scripts'], array($this->root_web.'/libraries/'.$this->autoload_libraries[$i])); 
		}
		
		// Load themes
		$themes = array_keys($this->themes);
		foreach ($themes as $theme) {
			$this->_load_theme($theme);
		}
		
		$this->styles	= implode("\n", $this->styles)."\n";
		$this->scripts	= implode("\n", $this->scripts)."\n";
		
		// Load modules, run initial hooks to establish full set of module metadata
		$modules = array_keys($this->modules);
		foreach ($modules as $module) {
			$this->_load_module($module);
		}
		
		// Load a module page
		foreach ($modules as $module)
		{
			// Set current page	
			if (array_key_exists('hook_pages', $this->modules[$module])
			&&	array_key_exists($this->args['path'], $this->modules[$module]['hook_pages']))
			{
				
				// TODO: ??? Add preprocess hook
				// Autoload these blocks
				for ($i=0; $i < count(self::$autoload_modules_1); $i++) {
					
					// Append hook_blocks
					if (array_key_exists('hook_blocks', $this->modules[self::$autoload_modules_1[$i]])) {
						foreach ($this->modules[self::$autoload_modules_1[$i]]['hook_blocks'] as $item_key => &$item_val) {
							if (!empty($item_val['autoload']) && $item_val['autoload']) {
								unset($item_val['autoload']);
								$item_key_mod															= self::$autoload_modules_1[$i].'_autoload_'.$item_key;
								$this->modules[$module]['hook_blocks'][$item_key_mod]					= &$item_val;
								$this->modules[$module]['hook_pages'][$this->args['path']]['blocks'][]	= $item_key_mod;
							}
						}
					}
					
					// Append $this->templates
					if (array_key_exists('hook_pages', $this->modules[self::$autoload_modules_1[$i]])) {
						foreach ($this->modules[self::$autoload_modules_1[$i]]['hook_pages'] as $item_key => &$item_val) {
							if (!empty($item_val['autoload']) && $item_val['autoload']) {
								$this->_prepare_template(self::$autoload_modules_1[$i], $item_val['template']);
							}
						}
					}
				}
				
				
				$page = &$this->modules[$module]['hook_pages'][$this->args['path']];
				
				// Run hooks w/callbacks for this module page
				$this->_run_hooks($module, true);
								
				// Load page template
				$item				= array();
				$item['template']	= $this->templates['page.tpl.php'];
				$item['page']		= $page;
				if (empty($item['page']['title'])) { $item['page']['title'] = ''; }
				$item['blocks']		= &$page['blocks']; // Move data around so this page template behaves like module page templates
				$item['styles']		= $this->styles;
				$item['scripts']	= $this->scripts;
				
				// Homepage
				if ($this->args['path']=='<front>'
				&&	array_key_exists('page-front.tpl.php', $this->templates)) {
					$item['template'] = $this->templates['page-front.tpl.php'];
				}
				
				// Process page template
				$this->_process_template($module, $item, array('hook_pages', $this->args['path']));
				print $item['output'];
				die();
				
			}
		}
		
		
		
		header('Location: /pages/404');
		die();
		
	}
		
	/*
	 * Global error handler
	 * 
	 * */
	public static function handle_error($code, $string, $file, $line, $context)
	{
		$msg = '';
		
		switch ($code)
		{
			case E_WARNING :
			case E_USER_WARNING :
				$msg .= 'Warning: ';
				break;
				
			case E_USER_ERROR :
			case E_RECOVERABLE_ERROR :
			case E_ALL :
				$msg .= 'Error: ';
				break;
				
			default :
				$msg .= 'Notice: ';
				break;
		}
		
		if (!empty($string))	{ $msg .= $string; }
		if (!empty($line))		{ $msg .= ' on line '.$line; }
		if (!empty($file))		{ $msg .= ' in '.$file; }
		if (!empty($msg))		{ self::$messages[] = $msg; return $msg; }
		
		return false;
	}
	
	
	
	/*
	 * Load dependencies (modules + themes)
	 * 
	 * */
	private function _load_dependencies($key, &$obj, $callback)
	{
		$mode = null;
		switch ($obj) {
			case $this->themes	: $mode = 'themes'; break;
			case $this->modules	: $mode = 'modules'; break;
		}
		
		if (array_key_exists('dependencies', $obj[$key]))
		{	
			$dependencies = $obj[$key]['dependencies'];
			for ($i=0; $i < count($dependencies); $i++)
			{
				$dependency = $dependencies[$i];
				
				 // Check for self dependency
				if ($key !== $dependency)
				{
					// Check for circular dependencies
					if (array_key_exists('dependencies', $obj[$dependency]))
					{
						// Loop dependency's dependencies and check against $key
						for ($j=0; $j < count($obj[$dependency]['dependencies']); $j++) {
							if ($key == $obj[$dependency]['dependencies'][$j]) {
								
								self::handle_error(
									E_USER_ERROR,
									vsprintf('Circular dependency between "%s" and "%s" detected. %s not loaded.', array($key, $dependency, ucwords($mode))),
									null,
									null,
									null
								);
								
								return false;
							}
						}
					}
					
					$this->{$callback}($dependency);
				}
			}
		}
		
		return true;
	}
	
	/*
	 * Load a theme
	 * 
	 * */
	private function _load_theme($key)
	{
		// Load dependencies
		if (!$this->_load_dependencies($key, $this->themes, '_load_theme')) {
			return false;
		}
		
		if (!$this->themes[$key]['loaded'])
		{
			// Add theme templates
			self::_find_files($this->themes[$key]['path'], 'php', $files);
			for ($i=0; $i < count($files); $i++) {
				$path = $files[$i]['dirname'].DIRECTORY_SEPARATOR.$files[$i]['basename'];
				$this->templates[$files[$i]['basename']] = $path;
			}
			
			// Set web-ready path
			$path = str_ireplace($this->root_dir, '', $this->themes[$key]['path']);
			$path = str_ireplace(DIRECTORY_SEPARATOR, '/', $path).'/';
			$path = $this->root_web.$path;
			
			// Add theme styles
			if (array_key_exists('styles', $this->themes[$key])) {
				foreach ($this->themes[$key]['styles'] as $media => $href) {
					$this->styles[] = vsprintf($this->html['styles'], array($path.$href, $media));
				}
			}
			
			// Add theme scripts
			if (array_key_exists('scripts', $this->themes[$key])) {
				foreach ($this->themes[$key]['scripts'] as $src) {
					$this->scripts[] = vsprintf($this->html['scripts'], array($path.$src));
				}
			}
			
			$this->themes[$key]['loaded'] = true;
		}
	}
	
	/*
	 * Load a module
	 * 
	 * */
	private function _load_module($key)
	{
		// Load dependencies
		if (!$this->_load_dependencies($key, $this->modules, '_load_module')) {
			return false;
		}
		
		// Instantiate module + dependencies
		if (!$this->modules[$key]['loaded'])
		{
			$this->modules[$key]['path_class'] = $this->modules[$key]['path'].DIRECTORY_SEPARATOR.$key.'.module';
			
			if (file_exists($this->modules[$key]['path_class']))
			{
				require_once($this->modules[$key]['path_class']);
				
				$this->{$key}			= new $key($this->args);
				$this->{$key}->messages	= &self::$messages;
				
				if (array_key_exists('dependencies', $this->modules[$key])) {
					for ($i=0; $i < count($this->modules[$key]['dependencies']); $i++) {
						$this->{$key}->{$this->modules[$key]['dependencies'][$i]} = &$this->{$this->modules[$key]['dependencies'][$i]};				
					}
				}
				
				$this->_run_hooks($key, false);
				
				$this->modules[$key]['loaded'] = true;
				
				return true;
			}
		}
		
		return false;
	}
	
	
	
	/*
	 * Run all hooks for a module
	 * 
	 * */
	private function _run_hooks($key, $do_callbacks = false)
	{
		$hooks = array(
			// Run hooks in this order!!!
			'hook_blocks',
			'hook_pages',
		);
		
		for ($i=0; $i < count($hooks); $i++) {
			if (method_exists($this->{$key}, $hooks[$i])) {
				$this->_run_hook($key, $hooks[$i], $do_callbacks);
			}
		}
	}
	
	/*
	 * Run a hook for a module
	 * 
	 * */
	private function _run_hook($key, $hook, $do_callbacks = false)
	{
		$items = array();
		
		if (!$do_callbacks)
		{
			// Get hook metadata
			$items = call_user_func(array(
					$this->{$key},
					$hook
				));
				
			// Unique processing per $hook
			switch ($hook)
			{
				case 'hook_blocks' :
					
					// Add module key to local $item; this is necessary for callback processing
					foreach ($items as $item_key => &$item_val) {
						if (!array_key_exists('module', $item_val)) {
							$item_val['module'] = $key;
						}
					}
					
					break;
				case 'hook_pages' :
					
					$item_keys = array_keys($items);
					foreach ($item_keys as $item_key) {
						
						// Make sure that returned page paths don't already exist
						if (self::array_key_exists_r($item_key, $this->modules)) {
							
							self::handle_error(
								E_USER_ERROR,
								vsprintf('%s->%s["%s"] path is not unique; ignoring item, ', array($key, $hook, $item_key)),
								$this->modules[$key]['path_class'],
								null,
								null
							);
							
							// Remove page from module
							unset( $items[$item_key] );
						}
					}
					
					break;
			}
			
		}
		else
		{
			// Get data from !$do_callbacks processing
			$items = &$this->modules[$key][$hook];
			
			// Unique processing per $hook
			switch ($hook)
			{
				case 'hook_blocks' :
					
					break;
				case 'hook_pages' :
					
					// Process current page and any blocks that have been assigned
					if (array_key_exists($this->args['path'], $items))
					{
						$item = &$items[$this->args['path']];
						
						if (array_key_exists('blocks', $item))
						{
							$item['blocks'] = array_flip($item['blocks']);
							
							foreach ($item['blocks'] as $block_key => &$block_val)
							{
								$this->_process_template(
									$this->modules[$key]['hook_blocks'][$block_key]['module'],
									$this->modules[$key]['hook_blocks'][$block_key],
									array('hook_blocks', $block_key)
								);
								
								$block_val = $this->modules[$key]['hook_blocks'][$block_key]['output'];
							}
						}
						
						// TODO: PAGES MODULE
						if (empty($item['autoload'])) {
							$this->_process_template(
								$key,
								$item,
								array($hook, $this->args['path'])
							);
						}
					}
					
					break;
			}
			
		}
		
		// Ref $items in the module array
		$this->modules[$key][$hook]	= &$items;
		
		// Ref $items in the local instance of module
		// (This data is now available to local module object instances, which are ref'd by mod dependencies)
		$this->{$key}->{$hook}		= &$items;
	}
	
	/*
	 * Prepare a template for inclusion
	 * 
	 * */
	private function _prepare_template($key, &$template)
	{
		// Prep template with full path
		$template		= str_replace('/', DIRECTORY_SEPARATOR, $this->modules[$key]['path'].DIRECTORY_SEPARATOR.$template);
		$template_key	= basename($template);
		
		// Add module template if it doesn't already exist in global collection
		if (!array_key_exists($template_key, $this->templates)) {
			$this->templates[$template_key] = $template;
		}
		
		// If it does (e.g. theme override), switch to existing template	
		$template = $this->templates[$template_key];
	}
	
	/*
	 * Process a template for a module
	 * 
	 * */
	private function _process_template($key, &$item, $debug = array())
	{
		// For system mods, assign internal vars for use in callbacks
		if (array_key_exists('package', $this->modules[$key])
		&&	$this->modules[$key]['package'] == 'core') {
			$this->{$key}->modules	= &$this->modules;
			$this->{$key}->themes	= &$this->themes;
		}
		
		
		if (empty($item['data']) 
		&& !empty($item['callback'])
		&& method_exists($this->{$key}, $item['callback']))
		{
			// Callback to module function
			// Args are accessible in the target mod's constructor
			$item['data'] = call_user_func(
				array($this->{$key}, $item['callback'])
			//	,$item['callback_args'] // TODO: process callback_args array
			);
		}
		
		if (empty($item['output'])
		&& !empty($item['template']))
		{
			$output = null;
			
			$this->_prepare_template($key, $item['template']);
			
			if (!file_exists($item['template']))
			{
				$output	= vsprintf('%s->%s["%s"]["template"], no such file: %s', array($key, $debug[0], $debug[1], $item['template']));
				$output	= self::handle_error(
					E_USER_ERROR,
					$output,
					$this->modules[$key]['path_class'],
					null,
					null
				);
			}
			else
			{
				// Process template in a static context, using $item as arg
				// We want to forbid the use of $this inside of templates, not expose Bootstrap details, etc.
				$output = self::_process_template_static($item);
			}
			
			$item['output'] = $output;
		}
	}
	
	private static function _process_template_static(&$item)
	{
		// Set local variables
		foreach ($item as $key => $val) {
			if ($key !== 'item') {
				$$key = $val;
			}
		}
		
		ob_start();
		$output = implode('', file($item['template']));
		$check	= eval(" ?>".$output."<?php return true; "); /* \$e = error_get_last(); if (!empty(\$e)) { print_r(\$e); } */
		$output	= ob_get_contents();
		ob_end_clean();
		
		// Handle template exceptions
		if (!$check)
		{
			$ex = error_get_last();
			
			if (!empty($ex)) {
				$output = self::handle_error(
					E_USER_ERROR,
					$ex['message'],
					$item['template'],
					$ex['line'],
					null
				);
			}
			else {
				$output = self::handle_error(
					E_USER_ERROR,
					'Unknown parse error',
					$item['template'],
					null,
					null
				);
			}
		}
		
		return $output;
	}
	
	
	
	/*
	 * Get .info file data
	 * 
	 * */
	private static function _get_info($path)
	{
		self::_find_files($path, 'info', $files);
		
		$collection = array();
		
		for ($i=0; $i < count($files); $i++)
		{
			$path	= $files[$i]['dirname'].DIRECTORY_SEPARATOR.$files[$i]['basename'];
			$data	= file_get_contents($path);
			$lines	= explode("\n", $data);
			$data	= array();
			
			// TODO: ??? Make these loops more atomic and just parse .info as PHP
			
			// Add standard key/val pairs to $data
				for ($j=0; $j < count($lines); $j++) {
					$lines[$j] = trim($lines[$j]);
					if (!empty($lines[$j])) {
						$pair		= explode('=', $lines[$j], 2);
						$key		= trim($pair[0]);
						$val		= rtrim(ltrim(trim($pair[1]), '"'), '"');
						$data[$key] = $val;
					}
				}
				
				if (stristr($path, 'modules'.DIRECTORY_SEPARATOR))	{ $data['type'] = 'module'; }
				if (stristr($path, 'themes'.DIRECTORY_SEPARATOR))	{ $data['type'] = 'theme'; }
				
				$data['loaded']	= false;
				$data['path']	= dirname($path);
				
				
			// Auto-add these Bootstrap-defined dependencies
				if ($data['type'] == 'module') {
					if (!in_array($files[$i]['filename'], self::$autoload_modules_1)) {
						$data['dependencies'] = implode(',', self::$autoload_modules_1) . (array_key_exists('dependencies', $data) ? ','.$data['dependencies'] : '');
					}
				}
				
			// Apply special processing for some keys
				$keys = array_keys($data);
				
			// Parse "dependencies"
				if (in_array('dependencies', $keys)) {
					$data['dependencies'] = explode(',', $data['dependencies']);
					array_walk($data['dependencies'], 'self::str_trim');
				}
				
			// Parse arrays (e.g. "regions", "styles")
				$arrays = array();
				foreach ($keys as $key) {
					if (stripos($key, '[')) {
						$val			= $data[$key]; unset($data[$key]); // Value isolated; remove cruft from $data
						$key			= str_ireplace(array(" ","\t", "'", "\""), "", $key); // More cruft removal if user is stupid
						$arrays[$key]	= $val;
					}
				}
				if (!empty($arrays)) {
					foreach ($arrays as $key => $val) {
						$outer	= substr($key, 0, stripos($key, '['));
						$subs	= substr($key, strlen($outer));
						$parts	= explode('][', $subs);
						
						if ($parts[0] == '[]') {
							// list array
							$data[$outer][] = $val;
						}
						else {
							// associative array
							$parts	= str_ireplace(array("[","]"), "", $parts); // Clean up more cruft because I'm stupid
							$expr	= '';
							for ($j=0; $j < count($parts); $j++) {
								$expr .= '["'.$parts[$j].'"]';
							}
							$expr = '$data["'.$outer.'"]'.$expr.' = "'.$val.'";';
							eval( $expr );
						}
					}
				}
			
			
			$collection[$files[$i]['filename']] = $data;
		}
		
		return $collection;
	}
	
	/*
	 * Recursively scan a directory for $extension files; fill $collection
	 * 
	 * */
	private static function _find_files($path, $extension, &$collection = array())
	{
		if ($handle = opendir($path)) {
			while (($file = readdir($handle)) !== false) {
				if ($file !== '.' && $file !== '..') {
					
					$current	= $path.DIRECTORY_SEPARATOR.$file;
					$filetype	= filetype($current);
					
					if ($filetype=='dir') {
						self::_find_files($current, $extension, $collection);
					}
					else {
						$info = pathinfo($current);
						if ($info['extension']==$extension) {
							$collection[] = $info;
						}
					}
				}
			}
			closedir($handle);
		}
	}
	
	
	
	/*
	 * Utility functions
	 * 
	 * */
	private static function str_trim(&$value)
	{
		$value = trim($value);
	}
	
	private static function array_key_exists_r($needle, $haystack)
	{
		$result = array_key_exists($needle, $haystack);
		if ($result) { return $result; }
		
		foreach ($haystack as $v) {
			if (is_array($v)) {
				$result = self::array_key_exists_r($needle, $v);
			}
			
			if ($result) return $result;
		}
		
		return $result;
	}
	
}