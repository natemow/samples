<?php
// Utility functions
	
	function strip_invalid_xml($value)
	{
		$ret = "";
		$current;
		if (empty($value)) { return $ret; }
		
		$length = strlen($value);
		for ($i=0; $i < $length; $i++)
		{
			$current = ord($value{$i});
			if (($current == 0x9) ||
				($current == 0xA) ||
				($current == 0xD) ||
				(($current >= 0x20) && ($current <= 0xD7FF)) ||
				(($current >= 0xE000) && ($current <= 0xFFFD)) ||
				(($current >= 0x10000) && ($current <= 0x10FFFF))
				) {
				$ret .= chr($current);
			}
			else {
				$ret .= " ";
			}
		}
		
		return $ret;
	}	
	
	function object_to_array($arrObjData, $arrSkipIndices = array())
	{
	    $arrData = array();
	   
	    // if input is object, convert into array
	    if (is_object($arrObjData)) {
	        $arrObjData = get_object_vars($arrObjData);
	    }
	   
	    if (is_array($arrObjData)) {
	        foreach ($arrObjData as $index => $value) {
	            if (is_object($value) || is_array($value)) {
	                $value = object_to_array($value, $arrSkipIndices); // recursive call
	            }
	            if (in_array($index, $arrSkipIndices)) {
	                continue;
	            }
	            $arrData[$index] = $value;
	        }
	    }
	    return $arrData;
	}
	
	function fill_from_attributes(&$data_in, &$data_out, $tagname, $fields, $key_field)
	{
		for ($i=0; $i < count($data_in[$tagname]); $i++) {
			$row = array();
			for ($f=0; $f < count($fields); $f++) {
				$row[$fields[$f]] = $data_in[$tagname][$i]['@attributes'][$fields[$f]];
			}
			
			// Okay...not totally a fill_from_attributes; save a loop and a whale!
			switch ($tagname)
			{
				case 'entry' :
					
					// Set the post excerpt + body, set new img src attribute
					$row['excerpt']		= (array_key_exists('excerpt', $data_in[$tagname][$i]) ? $data_in[$tagname][$i]['excerpt'] : '');
					$row['text']		= (array_key_exists('text', $data_in[$tagname][$i]) ? $data_in[$tagname][$i]['text'] : '');
					$row['text_more']	= (array_key_exists('text_more', $data_in[$tagname][$i]) ? $data_in[$tagname][$i]['text_more'] : '');
					
					replace_attr_val($row['excerpt'], 'src', 'http://fieldnotes.unicefusa.org/', '/wp-content/uploads/');
					replace_attr_val($row['text'], 'src', 'http://fieldnotes.unicefusa.org/', '/wp-content/uploads/');					
					replace_attr_val($row['text_more'], 'src', 'http://fieldnotes.unicefusa.org/', '/wp-content/uploads/');
					
					replace_quotes($row['excerpt']);
					replace_quotes($row['text']);
					replace_quotes($row['text_more']);
					
					// Inject the <!--more--> tag so WP handles teasers correctly
					if (!empty($row['excerpt'])) {
						$row['text'] = $row['excerpt'] . ' <!--more--> ' . $row['text'];
					}
					if (!empty($row['text_more'])) {
						$row['text'] .= $row['text_more'];
					}
										
					break;
				case 'comment' :
					
					$row['text']	= (array_key_exists('text', $data_in[$tagname][$i]) ? $data_in[$tagname][$i]['text'] : '');
					
					replace_quotes($row['text']);
					
					break;
			}
			

			
			
			if (!empty($key_field)) {
				$data_out[$tagname][ $data_in[$tagname][$i]['@attributes'][$key_field] ] = $row;
			}
			else {
				$data_out[$tagname][] = $row;
			}
		}
	}
	
	function parse_mt_date($date)
	{
	//	$date	= $entry['authored_on']; //20110715093009    Y-m-d H:i:s
		$yy		= substr($date, 0, 4);
		$mm		= substr($date, 4, 2);
		$dd		= substr($date, 6, 2);
		$hh		= substr($date, 8, 2);
		$mn		= substr($date, 10, 2);
		$ss		= substr($date, 12, 2);
		
		return date('Y-m-d H:i:s', strtotime($yy.'-'.$mm.'-'.$dd.' '.$hh.':'.$mn.':'.$ss));		
	}
	
	function replace_attr_val(&$subject, $attr, $old_val, $new_val)
	{
		/*
		$pattern		= sprintf('/\s(%s)=["\']?\/?(?!(https?:))([^>"\'\s]+)/i', $attr);
		$replacement	= sprintf(' $1="%s$2"', $new_val);
		$subject		= preg_replace($pattern, $replacement, $subject);
		*/
		
		$subject = str_ireplace($attr.'="'.$old_val, $attr.'="'.$new_val, $subject);
	}
	
	function replace_quotes(&$subject)
	{
		global $wpdb;
		
		$subject	= str_replace( array("\x82", "\x84", "\x85", "\x91", "\x92", "\x93", "\x94", "\x95", "\x96", "\x97"), array("&#8218;", "&#8222;", "&#8230;", "&#8216;", "&#8217;", "&#8220;", "&#8221;", "&#8226;", "&#8211;", "&#8212;"), $subject);
		$subject	= str_ireplace(array("`","Â´","â€™","Ã¢Â€Â™","&#8217;","&#180;","&apos;"), "'", $subject);
		$subject	= str_ireplace(array("Ì�","Ì‹","â€œ","â€�","&#8220;","&#8221;"), '"', $subject);
		$subject	= str_ireplace(array("â", "â�"), array("'"), $subject);
		$subject	= str_ireplace(array("â", "â"), array('"'), $subject);
		$subject	= str_ireplace(array("€", "â�‚¬"), array("&#8364;"), $subject);
		$subject	= html_entity_decode($subject, ENT_QUOTES, 'UTF-8');
		$subject	= str_ireplace(array("€", "â�‚¬"), array("&#8364;"), $subject);
		$subject	= str_ireplace(array("�‚©", "©", "�‚"), array("&copy;"), $subject);
		$subject	= str_ireplace(array("�‚®"), array("&reg;"), $subject);
		$subject	= str_ireplace(array("�"), array("&trade;"), $subject);
		$subject	= str_ireplace(array("�A"), array("a"), $subject);
		
		$subject	= str_ireplace(array("�‚¬", "�"), array(""), $subject);
		
		$subject	= $wpdb->escape($subject);
	}
	
	function flush_buffers()
	{
		print str_repeat(' ', 256);
		
		if (ob_get_length())
		{
			@ob_flush();
			@flush();
			@ob_end_flush();
		}
		
		@ob_start();
	}
	
	function show_progress($str, $append = '<br />')
	{
		print $str.$append;
		flush_buffers();
	}
	
	
	
	if (empty($_REQUEST['run'])) {
		show_progress('<a href="convert.php?run=1">Start</a>');
		return;
	}
	
	
// Now get sexy
	include_once('../../../../wp-load.php'); // Bootstrap WP
	include_once(ABSPATH . WPINC . '/registration.php');
	
	set_time_limit(0);
	ini_set('memory_limit', '1024M');
	
	show_progress('Starting...');
	show_progress('Reading in MT XML...');
	$xml 	= file_get_contents('Movable_Type-2011-08-26-23-47-34-Backup-1.xml');
	show_progress('UTF8 encoding XML...');
	$xml 	= utf8_encode($xml);
	show_progress('Cleaning up XML...');
	$xml 	= strip_invalid_xml($xml);
	$xml	= simplexml_load_string($xml);
	$parse	= object_to_array($xml);
	$data	= array();
	$debug	= false;
	
	unset($xml);
	
	show_progress('Prepping data...');
	
		
	// Get author data
		fill_from_attributes($parse, $data, 'author', array('id', 'created_on', 'name', 'password', 'nickname', 'email'), 'id');
		unset($parse['author']);
		
	// Get tag data
		fill_from_attributes($parse, $data, 'tag', array('id', 'name'), 'id');
		unset($parse['tag']);
		
	// Get category data
		fill_from_attributes($parse, $data, 'category', array('id', 'label'), 'id');
		unset($parse['category']);
		
	// Get entry data
		fill_from_attributes($parse, $data, 'entry', array('id', 'title', 'author_id', 'authored_on', 'status', 'allow_comments', 'allow_pings', 'basename'), 'id');
		unset($parse['entry']);
		
	// Get entry => tag data
		fill_from_attributes($parse, $data, 'objecttag', array('object_id', 'tag_id'), null);
		unset($parse['objecttag']);
		
	// Get entry => category data
		fill_from_attributes($parse, $data, 'placement', array('entry_id', 'category_id'), null);
		unset($parse['placement']);
		
	// Get comment data	
		fill_from_attributes($parse, $data, 'comment', array('id', 'entry_id', 'created_on', 'author', 'email', 'visible'), 'id'); // visible=1 Published
		unset($parse['comment']);
		
		unset($parse);
		
		
	// Only keep legit comments or this script will take days to run...
		$comments_to_keep = array();
		for ($c=0; $c < count($data['comment']); $c++) {
			if (intval($data['comment'][$c]['visible']) == 1) {
				$comments_to_keep[] = $data['comment'][$c];
			}
		}
		
		$data['comment'] = $comments_to_keep;
		
		
		
		show_progress('Adding data to WP...');
				
	// Associate data to entries, add to WP
		$fp = fopen('text_more.sql', 'w');
		
		foreach ($data['entry'] as $old_id => &$entry)
		{
			/*
			$post = array(
				'ID'			=> $entry['id'],
				'post_content'	=> $entry['text']
			);
			
			$id = wp_update_post($post);
			*/			
			$id = $entry['id'];
			
			fwrite(
				$fp,
				vsprintf("UPDATE wp_posts SET post_content = '%s' WHERE ID = %d;\r", array($entry['text'], $id) )
			);
			
			if (!empty($entry['text_more'])) {
				show_progress($id, ' ');
			}
		}
		unset($data);
		
		fclose($fp);
		
		show_progress('<br />Done!');
	
?>