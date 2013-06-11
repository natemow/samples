<?php
/**
 * @file
 * totem_common.efq.inc
 */

class TotemEntityFieldQuery {

  public $results;
  public $pager;

  public function __construct($vars) {

    // Set default EFQ params and merge with input.
    $vars_base = array(
      'return' => 'renderable', // 'renderable', 'entity', 'entity_id'
      'entity_type' => 'node',
      'bundle' => array(),
      'view_mode' => 'teaser',
      'property_conditions' => array(),
      'field_conditions' => array(),
      'meta_data' => array(),
      'tags' => array(),
      'page_limit' => 0,
      'count' => FALSE,
      'property_order_by' => array(),
      'field_order_by' => array(),
    );

    // Scrub any keys from input $vars not explicitly defined in $vars_base.
    $keys_vars = array_keys($vars);
    foreach ($keys_vars as $key) {
      if (!array_key_exists($key, $vars_base)) {
        unset($vars[$key]);
      }
    }

    $vars = array_merge($vars_base, $vars);


    // Instantiate our EFQ object.
    $query = new EntityFieldQuery();
    $query
      ->entityCondition('entity_type', $vars['entity_type']);

    if ($vars['entity_type'] == 'user') {
      // Users must be active.
      array_unshift($vars['property_conditions'], array(
        'column' => 'status',
        'value' => 1,
      ));
    }
    elseif ($vars['entity_type'] == 'node') {

      // 2012-08-28, natemow - Moving "bundle" to node queries exclusively
      // for now...queries choking for 'bundle'='user'
      if (!empty($vars['bundle'])) {
        $query->entityCondition('bundle', $vars['bundle'], (is_array($vars['bundle']) ? 'IN' : '='));
      }

      // Trigger hook_query_TAG_alters.
      $query->addTag('NODE_FILTER');
    }


    // Add propertyConditions.
    if (!empty($vars['property_conditions'])) {
      $property_conditions_base = array(
        'column' => NULL,
        'value' => NULL,
        'operator' => 'IN',
      );

      foreach ($vars['property_conditions'] as $ix => &$meta) {
        $meta = array_merge($property_conditions_base, $meta);

        if (!is_array($meta['value'])) {
          $meta['operator'] = '=';
        }

        $query->propertyCondition($meta['column'], $meta['value'], $meta['operator']);
      }
    }

    // Add fieldConditions.
    if (!empty($vars['field_conditions'])) {
      $field_conditions_base = array(
        'field' => NULL,
        'column' => NULL,
        'value' => NULL,
        'operator' => 'IN',
        'delta_group' => NULL,
        'language_group' => NULL,
      );

      foreach ($vars['field_conditions'] as $ix => &$meta) {
        $meta = array_merge($field_conditions_base, $meta);

        if (!is_array($meta['value'])) {
          $meta['operator'] = '=';
        }

        $query->fieldCondition($meta['field'], $meta['column'], $meta['value'], $meta['operator'], $meta['delta_group'], $meta['language_group']);
      }
    }

    // Add metaData.
    if (!empty($vars['meta_data'])) {
      $meta_data_base = array(
        'key' => NULL,
        'object' => NULL,
      );

      foreach ($vars['meta_data'] as $ix => &$meta) {
        $meta = array_merge($meta_data_base, $meta);

        $query->addMetaData($meta['key'], $meta['object']);
      }
    }

    // Add tags.
    if (!empty($vars['tags'])) {
      foreach ($vars['tags'] as $tag) {
        $query->addTag($tag);
      }
    }

    // Add propertyOrderBys.
    if (!empty($vars['property_order_by'])) {
      $property_order_by_base = array(
        'column' => NULL,
        'direction' => 'DESC',
      );

      foreach ($vars['property_order_by'] as $ix => &$meta) {
        $meta = array_merge($property_order_by_base, $meta);
        $query->propertyOrderBy($meta['column'], $meta['direction']);
      }
    }

    // Add fieldOrderBys.
    if (!empty($vars['field_order_by'])) {
      $field_order_by_base = array(
        'field' => NULL,
        'column' => NULL,
        'direction' => 'DESC',
      );

      foreach ($vars['field_order_by'] as $ix => &$meta) {
        $meta = array_merge($field_order_by_base, $meta);
        $query->fieldOrderBy($meta['field'], $meta['column'], $meta['direction']);
      }
    }

    // Force count.
    if ($vars['count']) {
      $query->count();
    }

    // Add paging.
    // MAKE SURE THIS ALWAYS STAYS RIGHT BEFORE THE ->execute() CALL SO COUNT IS ACCURATE!!!
    if (!empty($vars['page_limit'])) {
      $query->pager($vars['page_limit']);
    }

    // Execute query.
    $results = array();
    $entities = $query->execute();

    // If count, just set results = count and return.
    if ($query->count) {
      $this->results = $entities;
      return;
    }

    if (!empty($entities[$vars['entity_type']])) {
      $keys = array_keys($entities[$vars['entity_type']]);

      if (!empty($keys)) {

        switch ($vars['entity_type']) {
          case 'node':

            // Switch on return param
            switch ($vars['return']) {
              case 'entity_id':
                $results = $keys;
                break;

              case 'entity':
                $entity = node_load_multiple($keys);
                $results = $entity;
                break;

              case 'renderable':
              default:
                $entity = node_load_multiple($keys);
                $results = node_view_multiple($entity, $vars['view_mode']);
                $results = $results['nodes'];
                unset($results['#sorted']);
                break;
            }

            break;

          case 'user':

            // Switch on return param
            switch ($vars['return']) {
              case 'entity_id':
                $results = $keys;
                break;

              case 'entity':
                $entity = user_load_multiple($keys);
                $results = $entity;
                break;

              case 'renderable':
              default:
                $entity = user_load_multiple($keys);
                foreach ($entity as $entity_id => &$obj) {
                  $results[] = user_view($obj, $vars['view_mode']);
                }
                break;
            }

            break;

          case 'taxonomy_term':

            // Switch on return param
            switch ($vars['return']) {
              case 'entity_id':
                $results = $keys;
                break;

              case 'entity':
                $entity = taxonomy_term_load_multiple($keys);
                $results = $entity;
                break;

              case 'renderable':
              default:
                $entity = taxonomy_term_load_multiple($keys);
                foreach ($entity as $entity_id => &$obj) {
                  $results[] = taxonomy_term_view($obj, $vars['view_mode']);
                }
                break;

            }

            break;

          case 'comment':

            // Switch on return param.
            switch ($vars['return']) {
              case 'entity_id':
                $results = $keys;
                break;

              case 'entity':
                $entity = comment_load_multiple($keys);
                $results = $entity;
                break;

              case 'renderable':
              default:
                $entity = comment_load_multiple($keys);
                $results = comment_view_multiple($entity, $vars['view_mode']);
                unset($results['#sorted']);
                break;

            }

            break;

        }
      }
    }

    // Set final results.
    $this->results = $results;

    // Set custom pager.
    if (!empty($query->pager)) {
      $this->pager = _totem_common_get_pager(array(
        'quantity' => $query->pager['total'],
        'element' =>  $query->pager['element'],
      ));
    }

  }

}

// hook_query_TAG_alter implementations per flags set in EFQ object above.

/**
 * Implements hook_query_TAG_alter().
 */
function totem_common_query_NODE_FILTER_alter(QueryAlterableInterface $query) {
  global $user;

  $filters = _totem_common_session_get('filter');

  /*
   * In the case of count queries, node isn't necessarily present in the EFQ
   * wrapper...make sure it's always joined here.
   *
   * FALSE: for now, adding status=1 property_condition to count queries.
   * @see totem_common_block_view_active_communities()
   */
  //if (!in_array('node', $tables)) {
  //  $query->addJoin('INNER', 'node');
  //}

  //////////////////////////////////////////////////////////////////////////////
  // node.status filter
  /*
   * @see totem_common_node_submit()
   * @see totem_common_block_view_filter_node_status()
   * @see totem_common_query_NODE_FILTER_alter()
   */

  // User selected "published" filter (or nothing at all)
  if (empty($filters['node']['status'])) {
    // Default to published nodes.
    $condition = db_and()
      ->condition('node.status', 1);

    // Allow user to view published + own unpublished nodes.
    if (user_access('view own unpublished content')) {
      $condition = db_or()
        ->condition(db_and()
          ->condition('node.status', 0)
          ->condition('node.uid', $user->uid)
        )
        ->condition('node.status', 1);
    }

    $query->condition($condition);
  }
  // Return only user's unpublished nodes.
  else {
    // Default to published nodes.
    $condition = db_and()
      ->condition('node.status', 1);

    if (user_access('view own unpublished content')) {
      // We only want to filter subnode types (e.g. topic, media, event, etc.)
      $condition = db_or()
        ->condition(db_and()
          ->condition('node.status', 0)
          ->condition('node.uid', $user->uid)
          ->condition('node.type', array('page', 'community'), 'NOT IN')
        )
        ->condition(db_and()
          ->condition('node.status', 1)
          ->condition('node.type', array('page', 'community'), 'IN')
        );
    }

    $query->condition($condition);
  }
  //////////////////////////////////////////////////////////////////////////////

}
