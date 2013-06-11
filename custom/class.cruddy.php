<?php
abstract class Cruddy extends Database
{
	private	$Name,$Table,$Alias,$Columns,$Key,$Command;
	
	public function __construct()
	{
		parent::__construct(true, DB_CONN);
		
		$this->Name		= get_class($this);
		
		$table = ''; for ($i=0; $i < strlen($this->Name); $i++) { $table .= (($this->Name[$i]===strtoupper($this->Name[$i]) && $i > 0) ? '_' : '').$this->Name[$i]; }
		
		$this->Table	= strtolower($table);
	}

		private function get_type($sql_type)
		{
			// Set type specifier for later use with vsprintf()
			$type = $sql_type;
			if ($length = strpos($type, '(')) {
				$type = substr($type, 0, $length);
			}
			
			switch ($type) {
				case 'DECIMAL' : case 'FLOAT' : $type = "%f"; break;
				case 'INT' : case 'BIT' : $type = "%d"; break;
				default : $type = "'%s'"; break;
			}

			return $type;
		}
		
		private function prepare($command = 'SELECT', &$conditions = array(), &$fields = array())
		{
			$valid_commands	= array('SELECT', 'SAVE', 'DELETE');
			if (!in_array($command, $valid_commands)) {
				die("\"$command\" is not a valid Database command switch!");
			}
			
			// Get DESCRIBE results 1x per script execution
			if (empty($this->Columns))
			{
				$result			= self::query("DESCRIBE `".$this->Table."`;");
				
				if (!$result) { return false; }
				
				$this->Alias	= strtolower(substr($this->Table, 0, 1));
				$columns		= array();
				
				// Create array from DESCRIBE result
				for ($i=0; $i < count($result); $i++)
				{
					$name			= $result[$i]['field'];
					$columns[$name]	= array(
						'type'		=> strtoupper($result[$i]['type']),
						'null'		=> strtoupper($result[$i]['null']),
						'key'		=> strtoupper($result[$i]['key']),
						'default'	=> strtoupper($result[$i]['default']),
						'extra'		=> strtoupper($result[$i]['extra']),
					);
					
					// Set primary key var
					if ($columns[$name]['key'] == 'PRI' &&	$columns[$name]['extra'] == 'AUTO_INCREMENT') {
						$this->Key = $name;
					}
				}
				unset($result);
				
				$this->Columns = $columns;
			}

			
			
			$fieldsX = $fieldsX_values = $conditionsX = $conditionsX_values = array();	
			
			
			switch ($command)
			{
				case 'SELECT' :
					// Catch requests for "SELECT *..." and for empty field requests
					if (array_key_exists('*', $fields) || empty($fields)) {
						unset($fields);
						$fields = array_fill_keys(array_keys($this->Columns), '');	
					}
					break;
					
				case 'SAVE' :
					$command = (array_key_exists($this->Key, $fields) && !empty($fields[$this->Key])) ? 'UPDATE' : 'INSERT';
					
					if ($command == 'UPDATE') {
						// Save value to var
						$key_value = (array_key_exists($this->Key, $conditions) ? $conditions[$this->Key] : $fields[$this->Key]);
						// Make sure we don't update PK
						unset($fields[$this->Key]);
						// Make sure PK is included in conditions
						$conditions[$this->Key] = $key_value;
					} else {
						// No conditions required for INSERT; clear all params
						if (array_key_exists($this->Key, $fields)) unset($fields[$this->Key]);
						$conditions = array();
					}
					break;
			}
							
				// If PK is part of conditions, omit all others
				if (array_key_exists($this->Key, $conditions) && count($conditions)>0)
				{	
				//	$key_value				= $conditions[$this->Key];
				//	$conditions				= array();
				//	$conditions[$this->Key] = $key_value;
				}
				
				
				
			foreach ($fields as $key => &$value)
			{
				$type = self::get_type($this->Columns[$key]['type']);
				
				// Safety checks on the actual field value
			//	$value = mysql_real_escape_string($value, $this->Link);
				$fieldsX_values[] = $value;
				
				switch ($command)
				{
					case 'SELECT' : $fieldsX[] = $this->Alias.".`$key`"; array_pop($fieldsX_values); break;
					case 'INSERT' : $fieldsX["`$key`"] = $type; break;
					case 'UPDATE' : $fieldsX[] = $this->Alias.".`$key` = $type"; break;
					case 'DELETE' : /*do nothing*/ break;
				}
			}
			
			foreach ($conditions as $key => &$value)
			{
				$type = self::get_type($this->Columns[$key]['type']);
				
				// Safety checks on the actual field value
			//	$value = mysql_real_escape_string($value, $this->Link);
				
				if (is_array($value))
				{
					// Multiple values; force SQL "IN" operator in $command switch below
					array_pop($conditionsX_values);
					
					$symbol = $type;
					$type	= array();
					for ($i=0; $i<count($value); $i++) {
						$type[] 				= $symbol;
						$conditionsX_values[]	= $value[$i];
					}
				}
				else
				{
					// Single value; force SQL "=" operator in $command switch below
					$conditionsX_values[] = $value;
				}
				
				if (!empty($type))
				{
					switch ($command)
					{
						case 'SELECT' : $conditionsX[] = $this->Alias.".`$key` ".(is_array($type) ? "IN (".join(",",$type).")" : "= $type"); break;
						case 'INSERT' : /*do nothing*/ break;
						case 'UPDATE' : $conditionsX[] = $this->Alias.".`$key` ".(is_array($type) ? "IN (".join(",",$type).")" : "= $type"); break;
						case 'DELETE' : $conditionsX[] = "`$key` ".(is_array($type) ? "IN (".join(",",$type).")" : "= $type"); break;
					}
				}
			}
			
			switch ($command)
			{
				case 'SELECT' : $command = "SELECT " . join(",", $fieldsX) . " FROM `" . $this->Table . "` AS " . $this->Alias; break;
				case 'INSERT' : $command = "INSERT INTO `" . $this->Table . "` (" . join(",", array_keys($fieldsX)) . ") VALUES (" . join(",", array_values($fieldsX)) . ")"; break;
				case 'UPDATE' : $command = "UPDATE `" . $this->Table . "` " . $this->Alias . " SET " . join(", ", $fieldsX); break;
				case 'DELETE' : $command = "DELETE FROM `" . $this->Table . "`"; break;
			}
			
			if (count($conditionsX) > 0) {
				$command .= " WHERE " . join(" AND ", $conditionsX);
			}
			
			$command	   .= ";";
			$this->Command	= $command;
			$values			= array_merge($fieldsX_values, $conditionsX_values);
			$command		= vsprintf($command, $values);
			
			//print '<pre>';
			//print $command . '<br /><br />';
			//print_r($fieldsX);
			//print_r($fieldsX_values);
			//print_r($conditionsX);
			//print_r($conditionsX_values);
			//print_r($values);
			//print '</pre>';
			
			return $command;
		}
	
	
	protected function read(&$conditions = array(), &$fields = array(), &$order = array(), &$limit = null)
	{
		$command	= self::prepare('SELECT', $conditions, $fields);
		
		if (!empty($order)) {
			$command = substr($command, 0, strlen($command)-1);
		//	$command.=" ORDER BY $this->Alias.`".join(",$this->Alias.`", $order)."`;";
			$command.=" ORDER BY ".join(",", $order).";";
		}
		if (!empty($limit) && is_numeric($limit)) {
			$command = substr($command, 0, strlen($command)-1);
			$command.=" LIMIT $limit;";
		}
		
		$result = self::query($command);
		
		return $result;
	}
	
	protected function save(&$conditions = array(), &$fields = array())
	{
		$command	= self::prepare('SAVE', $conditions, $fields);
		$result		= self::exec($command);
		
		return $result;
	}
	
	private $BulkOptions = array('command'=>'', 'command_format'=>'', 'command_values'=>array());
	
	protected function bulk(&$conditions = array(), &$fields = array(), $threshold = 1)
	{
		if (count($this->BulkOptions['command_values'])===0) {
			self::prepare('SAVE', $conditions, $fields);
			
			$cmd_split	= stripos($this->Command, 'VALUES '); 
			$cmd 		= substr($this->Command, 0, $cmd_split);
			$cmd_fmt	= trim(substr($this->Command, $cmd_split+strlen('VALUES ')), ';');
			
			$this->BulkOptions['command'] 			= $cmd.' VALUES';
			$this->BulkOptions['command_format']	= $cmd_fmt;
		}
		else {
			unset($fields[$this->Key]);
		}
		
		if (count($this->BulkOptions['command_values']) % $threshold === 0)
		{
			$values	= &$this->BulkOptions['command_values'];
			if (count($values)>0) {
				$command	= $this->BulkOptions['command'].' '.join(',', $values).';';
				self::exec($command, false);
			}
			
			unset($command);
			unset($values);
			unset($this->BulkOptions['command_values']);
			
			// Clear processed records
			$this->BulkOptions['command_values'] = array();
		}
		
		$values = array_values($fields);
		$this->BulkOptions['command_values'][] = vsprintf($this->BulkOptions['command_format'], $values);
		
		return count($this->BulkOptions['command_values']);
	}
	
	protected function delete(&$conditions)
	{
		$command 	= self::prepare('DELETE', $conditions);
		$result		= self::exec($command);
		
		return;
	}
	
}
?>