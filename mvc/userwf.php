
<!-- saved from url=(0126)https://interactive.springloops.io/project/16023/svn/source/raw/26/trunk%2Ckohana%2Capplication%2Cclasses%2Cmodel%2Cuserwf.php -->
<html><head><meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1"></head><body><pre style="word-wrap: break-word; white-space: pre-wrap;">&lt;?php
defined('SYSPATH') or die('No direct script access.');

class Model_UserWf extends Model
{
	public function validate($array)
	{
        return Validate::factory($array)
					-&gt;filter(true, 'trim')
					-&gt;filter('email', 'strtolower')
					
					-&gt;rule('name_first', 'not_empty')
					-&gt;rule('name_last', 'not_empty')
					
					-&gt;rule('email', 'not_empty')
					-&gt;rule('email', 'regex', array('\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}\b'))
					
					-&gt;rule('password', 'not_empty')
					-&gt;rule('password', 'min_length', array('8'))
					-&gt;rule('password_confirm', 'matches', array('password'))
					
					-&gt;rule('zip', 'not_empty')
					-&gt;rule('zip', 'min_length', array('5'))
				;
	}
	
	public function save($array)
	{
		$id = DB::insert(array_keys($array))
				-&gt;values($array)
				-&gt;execute();
		
		return $id;
	}
	
	
}</pre></body></html>