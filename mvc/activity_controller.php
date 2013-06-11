<?php
class ActivityController extends AppController
{
	/*
	Note: See config/inflections.php [ $uninflectedPlural = array('activity') ]
	...this allows the controller to be named "Activity" vs. "Activities"
	*/
	var $name = 'Activity';

	function beforeRender() {
		if (!$this->pageTitle) {
			$this->pageTitle = __('ACTIVITY_BRAND', true) . ' Search';
		}
	}

	function index()
	{
		// Set tags for view
		$this->set('tags', $this->Lists->getActivityTags($this->Activity));

		// Set prior search param values for view
		$fields = array(
			'name','dt_start','dt_end',
			'price_min','price_max','count_tickets',
			'loc_country','loc_state','loc_city',
			'tag'
			);

			for ($i=0; $i<count($fields); $i++) {
				if ($value = parent::get_PersistentSearchParams($fields[$i])) {
					$this->set($fields[$i], $value);
				}
			}
	}

	function search()
	{
		$criteria			= $this->params['url']; unset($criteria['url']);
		$criteria_persist	= array();
		$conditions			= array('Activity.yn_enabled=1', 'Activity.dt_start <= now()', 'Activity.dt_end >= now()');
		$whitelist			= array();
		$keys 				= array_keys($criteria);

		for ($i=0; $i < count($keys); $i++)
		{
			$field_name		= $keys[$i];
			$field_value	= trim(str_replace("'", "\'", $criteria[$field_name]));
			//echo $field_name . ' = ' . $field_value .'<br/>';

			// Map friendly key name to db field name
			if (!empty($field_value))
			{
				try
				{

					switch ($field_name)
					{
						case 'dt_start' :
						case 'dt_end'	:
							if ($field_value = strtotime($field_value)) {
								$conditions[] = "(
									('".date('Y-m-d',	parent::sanitize($field_value))."' BETWEEN Activity.dt_start AND Activity.dt_end) AND
									('".date('H:i', 	parent::sanitize($field_value))."' BETWEEN DATE_FORMAT(Activity.dt_start, '%H:%i') AND DATE_FORMAT(Activity.dt_end, '%H:%i'))
									)";

								$field_value = date('m/d/Y H:i', parent::sanitize($field_value));
							}
							break;

						case 'price_min' :
							if ($field_value = floatval($field_value)) {
								$conditions[] = "(
									Activity.price >= ".parent::sanitize($field_value)."
									)";
							}
							break;

						case 'price_max' :
							if ($field_value = floatval($field_value)) {
								$conditions[] = "(
								Activity.price <= ".parent::sanitize($field_value)."
								)";
							}
							break;

						case 'count_tickets' :
							if ($field_value = floatval($field_value)) {
								$conditions[] = "(
									Activity.min_tickets >= ".parent::sanitize($field_value)." AND Activity.max_tickets <= ".parent::sanitize($field_value)."
									)";
							}
							break;

						case 'loc_city' :
							$conditions[] = "(
								Activity.loc_city LIKE '%".parent::sanitize($field_value)."%'
								)";
							break;

						case 'loc_state' :
							$conditions[] = "(
								Activity.loc_state LIKE '%".parent::sanitize($field_value)."%'
								)";
							break;

						case 'loc_country' :
							$conditions[] = "(
								Activity.loc_country LIKE '%".parent::sanitize($field_value)."%'
								)";
							break;

						case 'tag' :

							if (substr($field_value, strlen($field_value)-1, 1) == '|') {
								$field_value = substr($field_value, 0, strlen($field_value)-1);
							}

							$field_values	= explode('|', $field_value);
							$c				= "";

							for ($j=0; $j < count($field_values); $j++) {
								$c .= "Activity.description_tags LIKE '%".parent::sanitize($field_values[$j])."%'" .  (($j+1<count($field_values)) ? " OR " : "");
							}

							$conditions[] = "(".$c.")";

							$criteria[$field_name] = $field_values;
							break;

						case 'supplier' :
							$conditions[] = "(
								Activity.supplier_id = ".parent::sanitize($field_value)."
							)";
							break;

						case 'name' :
							$conditions[] = "(
								Activity.name LIKE '%".parent::sanitize($field_value)."%' OR
								Activity.description_tags LIKE '%".parent::sanitize($field_value)."%' OR
								Activity.description_short LIKE '%".parent::sanitize($field_value)."%' OR
								Activity.description_long LIKE '%".parent::sanitize($field_value)."%'
								)";

							break;
					}

					// Append session search string
					$criteria_persist[]	= $field_name.'='.urlencode($field_value);
				}
				catch (Exception $e)
				{
					// Some error parsing $field_value...remove that condition...
					array_pop($conditions);
				}
			}
		}


		// Execute search
		$this->paginate = array(
			'limit'		=> (empty($this->params['url']['ext']) ? 20 : 100), // Limit 20 for non-RSS requests
			'recursive'	=> 0,
			'page'		=> 1,
			'order'		=> "Activity.price DESC, Activity.name ASC",
			'fields'	=> "Activity.id,Activity.name,Activity.created,Activity.modified,Activity.price,Activity.dt_start,Activity.dt_end,Activity.description_short,Supplier.id,Supplier.name,Supplier.yn_acceptedterms,Supplier.yn_optintrx"
			);

		$activities = $this->paginate($this->Activity, $conditions, $whitelist);


		// If count==1, render detail
		if (count($activities)==1
		&&	empty($this->params['named']['page'])
		&&	empty($this->params['url']['ext'])
			)
		{
			$this->Activity->recursive = 1;

			$this->setFlash('DEFAULT_ONEDATA_SUCCESS', '');
			$this->view($activities[0]['Activity']['id']);
			$this->render('view');
			die();
		}
		elseif (
			count($activities) > 0
		&&	empty($this->params['url']['ext']))
		{
			// Set the persistent "Return to results" link
			parent::set_PersistentSearchParams( implode(parent::SearchParamsSeparator, $criteria_persist) );

			// Walk every result and determine if Transactions are allowed
			array_walk($activities, array('AppController', 'set_RuntimeActivityParams'));

			// Randomize results
			//srand();
			//shuffle($activities);
		}

		// Create a pretty array of all search params
		$c1 = array_values($criteria);
		$c2 = array();
		for ($i=0; $i<count($c1); $i++) {

			if (is_array($c1[$i])) {
				for ($j=0; $j<count($c1[$i]); $j++) {
					if (!empty($c1[$i][$j])) {
						$c2[] = $c1[$i][$j];
					}
				}
			}
			elseif (!empty($c1[$i])) {
				$c2[] = $c1[$i];
			}
		}

		// Set data for view
		$this->set('criteria',		$c2);
		$this->set('activities',	$activities);
	}



	function view($id = null)
	{
		$a = $this->viewActivity($id);

		$this->set('user',		$this->get_CurrentUser());
		$this->set('activity',	$a);
	}

		function viewActivity(&$id = null)
		{
			parent::view($this->Activity, $id);

			// Walk result and determine if Transactions are allowed
			$a = array($this->data);
			array_walk($a, array('AppController', 'set_RuntimeActivityParams'));

			$this->data = $a[0];

			if (!empty($this->params['named']['dt'])) {
				$this->data['Activity']['dt_selected'] = $this->params['named']['dt'];
			}

			return $this->data;
		}

	function add()
	{
		if (!empty($this->data))
		{
			$this->addeditPost($this->data);
		}

		$this->pageTitle = 'Add ' . __('ACTIVITY_BRAND', true);
		parent::add($this->Activity, '/suppliers/home');

		$user 				= $this->get_CurrentUser();
		$supplier			= $this->Activity->Supplier->read(null, $user['Supplier']['id']);

		$this->set('supplier', 		$supplier);
		$this->set('member',		$user);
		$this->set('activities',	$supplier['Activities']);
	}

	function edit($id = null)
	{
		if (!empty($this->data))
		{
			$this->addeditPost($this->data);
		}

		$this->pageTitle = 'Update ' . __('ACTIVITY_BRAND', true);
		parent::edit($this->Activity, $id, '/suppliers/home');

		$user 				= $this->get_CurrentUser();
		$supplier			= $this->Activity->Supplier->read(null, $user['Supplier']['id']);

		$this->set('supplier', 		$supplier);
		$this->set('member',		$user);
		$this->set('activities',	$supplier['Activities']);
	}

		private function addeditPost(&$data)
		{
			// Upload/crop 3 images
			for ($i=1; $i<4; $i++)
			{
				if (!empty($this->data['Activity']['image'.$i]['name']))
				{
					$this->data['Image']	= $this->data['Activity'];
					$this->data['Activity']['image'.$i]	= $this->Image->upload_image_and_thumbnail(
						$this->data,
						'image'.$i,
						Configure::read('Images.big'),
						Configure::read('Images.small'),
						Configure::read('Images.activity'),
						false
						);
				}
				else
				{
					// Exclude from SQL field list
					unset($this->data['Activity']['image'.$i]);
				}

				// Sleep for a while to force new microtime in ImageComponent
				usleep(100);
			}


			if ($data['Activity']['description_tags'] == __('HELP_EXAMPLE_ACTIVITYTAGS', true)) {
				$data['Activity']['description_tags'] = '';
			}
			if ($data['Activity']['description_short'] == __('HELP_EXAMPLE_ACTIVITYSHORTDESC', true)) {
				$data['Activity']['description_short'] = '';
			}
			if ($data['Activity']['description_long'] == __('HELP_EXAMPLE_ACTIVITYLONGDESC', true)) {
				$data['Activity']['description_long'] = '';
			}
			if (empty($data['Activity']['price'])) {
				$data['Activity']['price']		= 0;
			}
			if (empty($data['Activity']['min_tickets'])) {
				$data['Activity']['min_tickets']= 1;
			}
			if (empty($data['Activity']['max_tickets'])) {
				$data['Activity']['max_tickets']= 0;
			}
			if (empty($data['Activity']['dt_start'])) {
				$data['Activity']['dt_start']	= 0; //strtotime('0000-00-00 00:00:00');
			}
			if (empty($data['Activity']['dt_end'])) {
				$data['Activity']['dt_end']		= 0; //strtotime('0000-00-00 00:00:00');
			}

			$data['Activity']['dt_start']	= date('Y-m-d H:i:s', strtotime($data['Activity']['dt_start']));
			$data['Activity']['dt_end']		= date('Y-m-d H:i:s', strtotime($data['Activity']['dt_end']));

			// Set new defaults for Activity records going forward
				$default_fields = array();

				// Set list of fields to update
				if ($data['Supplier']['yn_defaultloc']) {
					$default_fields = array_merge($default_fields,
						array('loc_street', 'loc_city', 'loc_state', 'loc_zip', 'loc_country')
						);
				}
				if ($data['Supplier']['yn_defaultdesc']) {
					$default_fields = array_merge($default_fields,
						array('description_tags', 'description_short', 'description_long')
						);
				}
				if ($data['Supplier']['yn_defaulttrans']) {
					$default_fields = array_merge($default_fields,
						array('price', 'min_tickets', 'max_tickets')
						);
				}

				// Set default Supplier->Activity data
				$data['Supplier']['id'] = $data['Activity']['supplier_id'];
				for ($i=0; $i < count($default_fields); $i++) {
					$default_fields[$i] = 'default_'.$default_fields[$i];
					$data['Supplier'][$default_fields[$i]] = $data['Activity'][ substr($default_fields[$i], strlen('default_')) ];
				}

				// Save default Supplier->Activity data
				//print_r($data['Supplier']);
				$this->Activity->Supplier->save($data, false, $default_fields);
				//die();


			return $data;
		}


	function delete($id = null)
	{
		parent::delete($this->Activity, $id, '/suppliers/home');
	}


	function schedule()
	{
		$this->pageTitle = __('ACTIVITY_BRAND', true) . ' Scheduler';

		if ($user = $this->get_CurrentUser()) {
			if ($this->Roles->authorize(array(RolesComponent::USER), false)) {
				$this->setFlash(
					sprintf(__('MEMBER_LOGIN_SUCCESS', true), $user['Member']['name_first']),
					array('action'=>'/members/home', 'type'=>AppController::FlashNotice)
					);
        	}
		}
	}

}
?>