<?php
class OrdersController extends AppController
{
	var $name = 'Orders';
	var $uses = array('Order', 'Schedule', 'Member', 'MembersGroups');


	// Member checkout process starts/ends here!!!
	function add($step = 0)
	{
		$user		= $this->get_CurrentUser();
		$products	= null;

		if (!empty($this->data['Order']['Items']))
		{
			$schedule_ids = implode(',', $this->data['Order']['Items']['id']);

			// Get the Schedule "products" for this order
			$this->Schedule->unbindModel(array(
				'hasMany'=>array('Activity')
				));

			$this->Schedule->bindModel(array(
				'hasAndBelongsToMany'=>array(

					// Get the associated Groups for any Schedule selections
					'GroupsSchedule' => array(
						'className'		=> 'Groups',
						'joinTable'		=> 'schedules',
						'foreignKey'			=> 'id',
						'associationForeignKey'	=> 'group_id',
						'fields'		=> 'GroupsSchedule.id,GroupsSchedule.name',
						'conditions'			=> 'Schedule.id IN (' . $schedule_ids . ')'
					),

					// Get the Scheduled Activity selections
					'Activities' => array(
						'className'				=> 'Activity',
						'joinTable'				=> 'schedules',
						'foreignKey'			=> 'id',
						'associationForeignKey' => 'activity_id',
						'fields'				=> 'Activities.id,Activities.supplier_id,Activities.name,Activities.dt_start,Activities.dt_end,Activities.image1,Activities.price',
						'conditions'			=> 'Schedule.id IN (' . $schedule_ids . ')',
						'order'					=> 'Schedule.dt_selected'
					)
				)
			));


			if ($scheduled = $this->Schedule->find('all', array(
						'conditions'	=> 'Schedule.id IN (' . $schedule_ids . ')', //' AND Schedule.member_id = ' . $user['Member']['id'] . '',
						'order'			=> 'Schedule.dt_selected'
						)
					)
				)
			{
				$products = $scheduled;
			}


			if (!$this->Order->create($this->data)
			||	!$this->Order->validates())
			{
				$this->setFlash('ORDERS_CART_FAILURE', array('errors'=>$this->Order->validationErrors));
				$step--;
			}

			// Do step-specific stuff here:
			if ($step < 0) { $step = 0; }
			switch ($step)
			{
				case 0 :

					// Check the selections for associated Group Member Schedule line items
					for ($i=0; $i<count($products); $i++)
					{
						$products[$i]['Schedule']['group_name']		= '';
						$products[$i]['Schedule']['group_items']	= array();

						if (!empty($products[$i]['Schedule']['group_activity_key']))
						{
							// Set the name of this Group for the Scheduled Activity
							for ($g=0; $g<count($products[$i]['GroupsSchedule']); $g++) {
								if ($products[$i]['GroupsSchedule'][$g]['id']===$products[$i]['Schedule']['group_id']) {
									$products[$i]['Schedule']['group_name'] = $products[$i]['GroupsSchedule'][$g]['name'];
								}
							}
							unset($products[$i]['GroupsSchedule']);

							$this->Schedule->unbindModel(array(
								'hasMany'=>array('Activity')
								));

							$this->Schedule->bindModel(array(
								'hasAndBelongsToMany'=>array(
									'Member'=>array(
										'joinTable'				=> 'schedules',
										'foreignKey'			=> 'id',
										'associationForeignKey' => 'member_id',
										'fields'				=> 'Member.name_first,Member.name_last,Member.name_screen'
										)
									)
								)
							);

							$products_from_group = $this->Schedule->find('all', array(
								'conditions'	=> "Schedule.group_activity_key = '" . $products[$i]['Schedule']['group_activity_key'] . "' AND Schedule.member_id != " . $user['Member']['id'] . " AND Schedule.order_id IS NULL AND Schedule.order_item_id IS NULL",
								'order'			=> ''
								)
							);

							unset($products_from_group[0]['Schedule']);
							$products[$i]['Schedule']['group_items'] = $products_from_group[0];
							unset($products_from_group);
						}
					}

					break;

				case 1 :
					break;

				case 2 :

					// Save Order
					$this->Order->save();

					// Save Items
					for ($i=0; $i < count($products); $i++)
					{
						$product	= $products[$i]['Activities'][0];
						$item		= array(
							'id'			=> 0,
							'order_id'		=> $this->Order->id,
							'activity_id'	=> $product['id'],
							'supplier_id'	=> $product['supplier_id'],
							'name'			=> $product['name'],
							'price'			=> $product['price'],
							'quantity'		=> 1
						);

						$this->Order->Items->save($item);

						// Update Schedule w/new ids
						$this->Schedule->execute("
							UPDATE schedules SET
							 order_id = "		. $this->Order->id . "
							,order_item_id = "	. $this->Order->Items->id . "
							WHERE id = "		. $product['Schedule']['id']
						);
					}

					//die();
					break;
			}

			$view = 'step'.$step;

		}

		if (empty($products))
		{
			$this->setFlash('ORDERS_CART_EMPTY', array('action'=>'/members/home', 'type'=>parent::FlashWarn));
		}

		$this->pageTitle = 'Checkout';
		$this->set('step',		($step+1));
		$this->set('user',		$user);
		$this->set('products',	$products);
		$this->render($view,	'checkout');
	}

	function view($id = null)
	{
		$this->pageTitle	= 'My Account';
		$user				= $this->get_CurrentUser();
		$conditions 		= 'Order.member_id = ' . $user['Member']['id'];
		$fields				= 'Order.id,Order.created,Order.total';
		$recursive			= 0;

		if ($this->Roles->authorize(array(RolesComponent::SUPPLIER), false))
		{
			$conditions = 'Order.id IN (SELECT i.order_id FROM orders_items i WHERE i.supplier_id = ' . $user['Supplier']['id'] . ')';

			$this->pageTitle = __('ACTIVITY_BRAND', true) . ' ' . __('SUPPLIER_BRANDS', true);
		}

		if (!empty($id))
		{
			$conditions			.= ' AND Order.id = ' . $id;
			$fields				.= ',Order.cc_trans_id,Order.cc_type,Order.cc_name,Order.cc_expire,Order.add_1,Order.add_2,Order.add_city,Order.add_state,Order.add_zip,Order.add_country';
			$recursive			= 1;
			$conditions_items	= null;

			// Limit supplier detail view to their Activities
			if ($this->Roles->authorize(array(RolesComponent::SUPPLIER), false)) {
				$conditions_items = 'Items.supplier_id = ' . $user['Supplier']['id'];
			}

			$this->Order->hasMany['Items']['conditions'] = $conditions_items;
		}

		// Get order data
		$orders = $this->Order->find('all', array(
			'conditions'	=> $conditions,
			'fields'		=> $fields,
			'order'			=> 'Order.created DESC',
			'recursive'		=> $recursive
			)
		);

		$this->set('orders', $orders);

		// Get all member data for the view
		parent::view($this->Member, $user['Member']['id']);
	}

}
?>