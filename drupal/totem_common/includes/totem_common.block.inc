<?php
/**
 * @file
 * totem_common.block.inc
 */

// Private helper functions.
/**
 * Custom function to return a renderable block.
 * Accepts block callback's $context args.
 */
function _totem_common_embed_block($module, $delta) {

  $block = NULL;
  $args  = func_get_args();

  // Remove $module and $delta from the arguments.
  array_shift($args);
  array_shift($args);

  $block_data = module_invoke($module, 'block_view', $delta, $args);

  if (!empty($block_data)) {
    $block = (object) $block_data;
  }
  else {
    $block = block_load($module, $delta);
  }

  if (!empty($block)) {
    if (empty($block->subject)) {
      $block->subject = NULL;
    }
    if (empty($block->title)) {
      $block->title = '<none>';
    }
    if (empty($block->module)) {
      $block->module = $module;
    }
    if (empty($block->delta)) {
      $block->delta = $delta;
    }
    if (empty($block->region)) {
      $block->region = '-1';
    }
    if (empty($block->weight)) {
      $block->weight = 0;
    }

    if (isset($block->content) && $block->content) {
      if (is_string($block->content)) {
        $block->content = array('#markup' => $block->content);
      }
    }

    // TODO: drupal_alter doesn't run in _block_render_blocks if content is
    // set; need to improve this globally in either _totem_common_embed_block
    // or directly in callback return structures.
    // Force alter calls here:
    $block_key = "{$block->module}_{$block->delta}";
    drupal_alter(array('block_view', "block_view_{$block_key}"), $block_data, $block);

    $block = array($block_key => $block);
    $block = _block_render_blocks($block);
    $block = _block_get_renderable_array($block);
  }

  return $block;
}

// Hook implementations.
/**
 * Implements hook_block_info().
 */
function totem_common_block_info() {

  $blocks = array();
  $site_name = check_plain(variable_get('site_name'));

  // Define a bunch of type-dependent blocks.
  $types = _totem_common_types_info(TRUE);
  // Remove page blocks...doesn't make sense to have blocks for those.
  unset($types['page']);
  // Remove items which are only for helping totem_common_menu().
  unset($types['recent']);
  unset($types['orphan']);

  $type_blocks = array();
  $ix = 0;
  foreach ($types as $key => $type) {

    // Derive some pages settings.
    $page_path_delimiter = "\r\n";
    $pages = array($type->path_plural);
    $pages_node = array("node/*/{$type->path_plural}", "node/*/{$type->path_plural}/*");
    $pages_user = array("user/*/{$type->path_plural}");

    $append_pages_node = FALSE;
    if (!in_array($type->type, array('community'))) {
      $append_pages_node = TRUE;
    }

    $pages = implode($page_path_delimiter, $pages);
    $pages_node = implode($page_path_delimiter, $pages_node);
    $pages_user = implode($page_path_delimiter, $pages_user);


    // Define type blocks.
    $type_blocks[$type->type] = array(
      // "Search [type]" blocks.
      'search_type' => array(
        'delta' => 'search_' . $ix,
        'region' => 'content',
        'status' => 1,
        'cache' => DRUPAL_NO_CACHE,
        'weight' => -5,
        'title' => '',
        'visibility' => BLOCK_VISIBILITY_LISTED,
        'pages' => $pages,
        'info' => t('@site_name - Search @type', array('@site_name' => $site_name, '@type' => $type->name_plural)),
        'properties' => array(
          'totem_search', 'search_type', $type,
        ),
      ),
      // "Add [type]" buttons.
      'button_add_type' => array(
        'delta' => 'button_add_' . $ix,
        'region' => 'sidebar_first',
        'status' => 1,
        'cache' => DRUPAL_NO_CACHE,
        'weight' => 10,
        'title' => '<none>',
        'visibility' => BLOCK_VISIBILITY_LISTED,
        // Also show this block on Community tab pages.
        'pages' => ($append_pages_node ? ($page_path_delimiter . $pages_node) : ''),
        'info' => t('@site_name - Add @type button', array('@site_name' => $site_name, '@type' => $type->name)),
        'properties' => array(
          'totem_common', 'button_add_type', $type, 'Add',
        ),
      ),
      // "List All [type]s" blocks.
      // (Formerly known as the "master" view in totem_common_node_community).
      'list_type' => array(
        'delta' => 'list_' . $ix,
        'region' => 'sidebar_first',
        'status' => 1,
        'cache' => DRUPAL_NO_CACHE,
        'weight' => 10,
        'title' => '<none>',
        'visibility' => BLOCK_VISIBILITY_LISTED,
        // Also show this block on Community tab pages.
        'pages' => ($append_pages_node ? $pages_node : ''),
        'info' => t('@site_name - @type list', array('@site_name' => $site_name, '@type' => $type->name_plural)),
        'properties' => array(
          'totem_common', 'list_type', $type,
        ),
      ),
      // "My [type]s" blocks.
      'my_type' => array(
        'delta' => 'my_' . $ix,
        'region' => 'sidebar_first',
        'status' => 1,
        'cache' => DRUPAL_NO_CACHE,
        'weight' => 10,
        'title' => '',
        'visibility' => BLOCK_VISIBILITY_LISTED,
        'pages' => $pages,
        'info' => t('@site_name - My @type', array('@site_name' => $site_name, '@type' => $type->name_plural)),
        'properties' => array(
          'totem_common', 'my_type', $type,
        ),
      ),
      // "Most Recent [type]s" blocks.
      'recent_type' => array(
        'delta' => 'recent_' . $ix,
        'region' => 'sidebar_first',
        'status' => 1,
        'cache' => DRUPAL_NO_CACHE,
        'weight' => 10,
        'title' => '',
        'visibility' => BLOCK_VISIBILITY_LISTED,
        'pages' => $pages,
        'info' => t('@site_name - Recent @type', array('@site_name' => $site_name, '@type' => $type->name_plural)),
        'properties' => array(
          'totem_common', 'recent_type', $type,
        ),
      ),
      // "Tags [type]" blocks.
      'tags_type' => array(
        'delta' => 'tags_' . $ix,
        'region' => 'sidebar_first',
        'status' => 1,
        'cache' => DRUPAL_NO_CACHE,
        'weight' => 10,
        'title' => '<none>',
        'visibility' => BLOCK_VISIBILITY_LISTED,
        // Also show this block on Community tab pages.
        'pages' => $pages . ($append_pages_node ? ($page_path_delimiter . $pages_node) : ''),
        'info' => t('@site_name - Tags @type', array('@site_name' => $site_name, '@type' => $type->name_plural)),
        'properties' => array(
          'totem_common', 'tags_type', $type,
        ),
      ),
      // "Featured [type]" blocks.
      'featured_type' => array(
        'delta' => 'featured_' . $ix,
        'region' => 'sidebar_first',
        'status' => 1,
        'cache' => DRUPAL_NO_CACHE,
        'weight' => 10,
        'title' => '',
        'visibility' => BLOCK_VISIBILITY_LISTED,
        // Also show this block on Community tab pages.
        'pages' => $pages . ($append_pages_node ? ($page_path_delimiter . $pages_node) : ''),
        'info' => t('@site_name - @type featured', array('@site_name' => $site_name, '@type' => $type->name_plural)),
        'properties' => array(
          'totem_common', 'featured_type', $type,
        ),
      ),
      // "Subscribed [type]" blocks.
      'subscribed_type' => array(
        'delta' => 'subscribed_' . $ix,
        'region' => 'sidebar_first',
        'status' => 1,
        'cache' => DRUPAL_NO_CACHE,
        'weight' => 5,
        'title' => '',
        'visibility' => BLOCK_VISIBILITY_LISTED,
        'pages' => $pages_user,
        'info' => t('@site_name - Member Subscribed @type', array('@site_name' => $site_name, '@type' => $type->name_plural)),
        'properties' => array(
          'totem_activity', 'subscribed_type', $type,
        ),
      ),
    );


    // Adjust block params per type.
    if ($type->type == 'community') {
      $type_blocks[$type->type]['button_add_type']['pages'] .= $page_path_delimiter . $pages;
      $type_blocks[$type->type]['recent_type']['pages'] .= $page_path_delimiter . "resources";
    }
    if ($type->type == 'user') {
      $type_blocks[$type->type]['recent_type']['pages'] .= $page_path_delimiter . "resources";
    }
    if ($type->is_resource) {
      $type_blocks[$type->type]['button_add_type']['pages'] .= $page_path_delimiter . $pages . $page_path_delimiter . "{$type->path}/*";
      $type_blocks[$type->type]['recent_type']['pages'] .= $page_path_delimiter . "{$type->path}/*";
      $type_blocks[$type->type]['subscribed_type']['pages'] = $pages;
      $type_blocks[$type->type]['subscribed_type']['weight'] = 10;
    }


    // Prune type blocks that don't make sense or that have no invoke target.
    if (in_array($type->type, array('user'))) {
      unset($type_blocks[$type->type]['button_add_type']);
      unset($type_blocks[$type->type]['tags_type']);
      unset($type_blocks[$type->type]['my_type']);
      unset($type_blocks[$type->type]['subscribed_type']);
    }
    if (!in_array($type->type, array('community'))) {
      unset($type_blocks[$type->type]['featured_type']);
    }
    if (in_array($type->type, array('community', 'media_collection'))) {
      unset($type_blocks[$type->type]['subscribed_type']);
    }
    if ($type->is_resource) {
      unset($type_blocks[$type->type]['my_type']);
      unset($type_blocks[$type->type]['list_type']);
      // TODO: Really want to allow "Resource" type subscription...going to
      // require a fair bit of refactor and Flag integration to make it
      // happen, though.
      unset($type_blocks[$type->type]['subscribed_type']);
    }

    $ix++;
  }


  // Now add blocks to return array.
  $weight_type = 0;
  foreach ($type_blocks as $type => &$type_blocks_group) {
    foreach ($type_blocks_group as $key => &$meta) {
      if ($meta['region'] == 'sidebar_first') {
        $meta['weight'] = ($meta['weight'] + $weight_type);
      }

      $meta['delta'] = 'embed_type_' . $meta['delta'];

      $blocks[$meta['delta']] = $meta;
    }

    $weight_type++;
  }




  // Define blocks for Community and Member overview screens.
  $overview_blocks = array(
    'totem_activity' => 'Recent Activity',
    'totem_common' => 'Communities',
    'totem_user' => 'Members',
    'totem_discuss' => 'Discussions',
    'totem_media' => 'Media',
    'totem_events' => 'Events',
  );

  $ix = -1;
  foreach ($overview_blocks as $module => $label) {

    $region = 'sidebar_first';
    $weight = 5;

    if ($label == 'Recent Activity') {
      $region = 'content';
    }

    // Community Overview block.
    $ix++;
    $blocks["overview_{$ix}"] = array(
      'region' => $region,
      'status' => 1,
      'cache' => DRUPAL_NO_CACHE,
      'weight' => $weight,
      'title' => '<none>',
      'visibility' => BLOCK_VISIBILITY_LISTED,
      // For limiting type to "community" in the block_node_type table.
      // @see _totem_common_install_blocks()
      'pages' => 'node/*',
      'info' => t('@site_name - Community Overview, @label', array('@site_name' => $site_name, '@label' => $label)),
      'properties' => array(
        $module, 'overview_community',
      ),
    );

    if ($module == 'totem_common') {
      // Remove "Communities" block for community overview screen...doesn't make sense.
      unset($blocks["overview_{$ix}"]);
      $ix--;

      // Add an additional block for member overview "Orphans" list.
      $ix++;
      $blocks["overview_{$ix}"] = array(
        'region' => $region,
        'status' => 0,
        'cache' => DRUPAL_NO_CACHE,
        'weight' => $weight,
        'title' => '<none>',
        'visibility' => BLOCK_VISIBILITY_LISTED,
        'pages' => 'members/*',
        'info' => t('@site_name - Member Overview, Orphaned Content', array('@site_name' => $site_name)),
        'properties' => array(
          $module, 'overview_user_orphan',
        ),
      );
    }

    // Member Overview block.
    $ix++;
    $blocks["overview_{$ix}"] = array(
      'region' => $region,
      'status' => 1,
      'cache' => DRUPAL_NO_CACHE,
      'weight' => $weight,
      'title' => '<none>',
      'visibility' => BLOCK_VISIBILITY_LISTED,
      'pages' => 'members/*',
      'info' => t('@site_name - Member Overview, @label', array('@site_name' => $site_name, '@label' => $label)),
      'properties' => array(
        $module, 'overview_user',
      ),
    );

    if ($module == 'totem_user') {
      // Remove "Members" block for member overview screen...doesn't make sense.
      unset($blocks["overview_{$ix}"]);
      $ix--;
    }

  }


  return $blocks;
}
/**
 * Implements hook_block_view().
 */
function totem_common_block_view($delta = '', $context = '') {

  $block = NULL;

  // View blocks.
  $callback = '_totem_common_block_view_' . $delta;
  if (function_exists($callback)) {
    $block = $callback($delta, $context);
  }

  if (empty($block)) {
    $type_block_info = &drupal_static(__FUNCTION__);
    if (empty($type_block_info)) {
      $type_block_info = module_invoke('totem_common', 'block_info');
    }

    if (array_key_exists($delta, $type_block_info)) {
      $block_view_args = $type_block_info[$delta]['properties'];
      if (!empty($block_view_args)) {
        $block_view_module = $block_view_args[0];
        $block_view_delta = $block_view_args[1];

        // Remove $module and $delta from the arguments.
        array_shift($block_view_args);
        array_shift($block_view_args);

        $block = module_invoke($block_view_module, 'block_view', $block_view_delta, $block_view_args);
      }
    }
  }

  return $block;

}

// Block view callbacks.
/**
 * Callback for "Add type" button blocks.
 */
function _totem_common_block_view_button_add_type($delta, $context) {

  global $user;

  if (!empty($context)) {
    $type = $context[0];
    $verb = $context[1];

    $text = t('@verb @label', array('@verb' => t($verb), '@label' => t($type->name)));
    $path = 'community/add/' . $type->path;
    if ($node = _totem_common_get_community_context_node()) {
      $path .= '/' . $node->nid;
    }

    // Check that user is allowed to add a node in this context.
    if (node_access('create', $type->type, $user)) {
      $link = _totem_common_modal_link(array(
        'text' => $text,
        'path' => $path,
        'class' => array('btn', 'corners', 'add-' . drupal_html_class($type->type)),
      ));

      $block = array(
        'content' => array(
          '#entity_type' => $type->type,
          '#type' => 'markup',
          '#markup' => $link,
          '#classes_array' => array('button-type', 'action', 'action-' . drupal_html_class($type->type)),
        ),
      );

      // Allow modules to alter this block.
      // Prefer this approach to hook_block_view_alter, as this view callback
      // could be triggered from a number of different unknown $deltas.
      drupal_alter('block_view_totem_common_button_add_type', $block);

      return $block;
    }
  }
}
/**
 * Callback for community list of all type entities block view.
 * (formerly known as the "master" view in totem_common_node_community)
 */
function _totem_common_block_view_list_type($delta, $context) {

  $node = _totem_common_get_community_context_node();
  $type = $context[0];

  if (!empty($node)) {
    $hook = "node_community_{$type->type}";
    $hook_arguments = array(
      'node' => $node,
      'page_limit' => NULL,
    );

    // Allow modules to alter params.
    $alter_type = __FUNCTION__;
    drupal_alter('totem_common_EFQ_params', $hook_arguments, $alter_type, $type->type);

    $items = module_invoke_all($hook, $hook_arguments);
    $query = _totem_common_efq_extract_query($items);


    $block = array(
      'content' => array(
        '#entity_type' => $type->type,
      ),
    );

    if (!empty($query) && !empty($query->results)) {
      // Instruct _totem_common_url_outbound_alter to append /nojs path part.
      foreach ($query->results as &$entity) {
        if ($entity['#entity_type'] == 'node') {
          $entity['#node']->url_outbound_set_ajax_path = TRUE;
        }
      }

      $block = array_merge_recursive($block, array(
        'subject' => t($type->name_plural),
        'content' => array(
          '#theme' => 'totem_common_paged_entities',
          '#query' => $query,
          '#classes_array' => array('list-' . drupal_html_class($type->type)),
        ),
      ));
    }

    // Allow modules to alter this block.
    drupal_alter('block_view_totem_common_list_type', $block);

    return $block;
  }
}
/**
 * Callback for My Type list block view.
 */
function _totem_common_block_view_my_type($delta, $context) {

  global $user;

  if (user_is_logged_in()) {
    $type = $context[0];
    $query = NULL;
    $hook = "user_community_{$type->type}";
    $items = module_invoke_all($hook, array(
      'account' => $user,
      'page_limit' => PAGE_SIZE_LISTS_TEASER,
    ));

    $query = _totem_common_efq_extract_query($items);

    if (!empty($query) && !empty($query->results)) {
      $path = "user/{$user->uid}/{$type->path_plural}";

      $subject = l(t('My Recent @label', array('@label' => $type->name_plural)), $path);

      $content = drupal_render($query->results);
      $content .= '<div class="clearfix"></div>';
      $content .= l(t('View My @label <span></span>', array('@label' => $type->name_plural)), $path, array('html' => TRUE, 'attributes' => array('class' => array('btn', 'corners', 'view-all'))));

      $block = array(
        'subject' => $subject,
        'content' => array(
          '#type' => 'markup',
          '#markup' => $content,
          '#classes_array' => array('my-type', 'my-' . drupal_html_class($type->type)),
        ),
      );

      return $block;
    }
  }
}
/**
 * Callback for Tags list block view.
 */
function _totem_common_block_view_tags_type($delta, $context) {

  $type = $context[0];
  $node_community = _totem_common_get_community_context_node();

  // Get/set all "Tags" terms.
  $terms = &drupal_static(__FUNCTION__);
  if (empty($terms)) {
    $vocab = taxonomy_vocabulary_machine_name_load('tags');
    $obj = _totem_common_efq(array(
      'return' => 'entity',
      'entity_type' => 'taxonomy_term',
      'property_conditions' => array(
        array('column' => 'vid', 'value' => $vocab->vid),
      ),
    ));

    $terms = $obj->results;
  }

  if (!empty($terms)) {

    // Get nodes that have these tags.
    $efq_params = array(
      'return' => 'entity',
      'entity_type' => 'node',
      'bundle' => $type->type,
      'property_conditions' => array(
        array('column' => 'status', 'value' => 1),
      ),
      'field_conditions' => array(
        array('field' => 'field_tags', 'column' => 'tid', 'value' => array_keys($terms)),
      ),
    );

    if (!empty($node_community)) {
      if ($type->type == 'community') {
        $efq_params['property_conditions'][] = array('column' => 'nid', 'value' => $node_community->nid);
      }
      else {
        $efq_params['field_conditions'][] = array('field' => 'field_community', 'column' => 'target_id', 'value' => $node_community->nid);
      }
    }

    $obj = _totem_common_efq($efq_params);

    $path = $type->path_plural;

    // Prune $terms.
    $items = array();
    foreach ($obj->results as $node) {
      $tags = field_get_items('node', $node, 'field_tags');
      foreach ($tags as $meta) {
        if (!array_key_exists($meta['tid'], $items)) {
          $term = taxonomy_term_load($meta['tid']);
          if (!empty($term)) {
            $items[$meta['tid']] = l($term->name, $path, array(
              'attributes' => array('class' => array('tag')),
              'query' => array('tags' => $term->name),
            ));
          }
        }
      }
    }
  }

  if (!empty($items)) {
    $block = array(
      'subject' => t('Tags'),
      'content' => array(
        '#theme' => 'item_list',
        '#items' => $items,
        '#title' => NULL,
        '#type' => 'ul',
        '#attributes' => array('class' => array('tags', 'tags-' . drupal_html_class($type->type), 'clearfix')),
      ),
    );

    return $block;
  }
}
/**
 * Callback for Recent Type list block view.
 */
function _totem_common_block_view_recent_type($delta, $context) {

  $type = $context[0];
  $types = array($type->type);
  $path = $type->path_plural;
  $nid_community = (!empty($context[1]) ? $context[1] : NULL);
  $entity_type = 'node';
  $order_column = 'changed';

  switch ($type->type) {
    case 'user':
      $entity_type = 'user';
      $order_column = 'created';
      break;

    case 'comment':
      $entity_type = 'comment';
      $path = NULL;
      break;
  }

  // Set recent [type] params.
  $efq_params = array(
    'entity_type' => $entity_type,
    'bundle' => $types,
    'page_limit' => PAGE_SIZE_LISTS_TEASER,
    'property_order_by' => array(
      array('column' => $order_column, 'direction' => 'DESC'),
    ),
  );

  // Set recent comment params.
  if ($type->type == 'comment' && !empty($nid_community)) {
    $comment_bundle = array();
    $comment_types = _totem_common_node_types();
    if (!empty($comment_types)) {
      foreach ($comment_types as $comment_type) {
        $comment_bundle[] = $comment_type->type;
      }
    }

    $efq_params_nodes_comments = array(
      'entity_type' => 'node',
      'bundle' => $comment_bundle,
      'return' => 'entity_id',
      // Remove sort by node creation time.
      'property_order_by' => array(),
      'tags' => array(
        'NODES_LAST_COMMENTED'
      ),
      'property_conditions' => array(
        array('column' => 'status', 'value' => 1),
      ),
      'field_conditions' => array(
        array('field' => 'field_community', 'column' => 'target_id', 'value' => $nid_community),
      )
    );

    $nids_comments = _totem_common_efq($efq_params_nodes_comments)->results;
    $efq_params['property_conditions'][] =  array('column' => 'nid', 'value' => (empty($nids_comments) ? array(0) : $nids_comments));
    $efq_params['property_conditions'][] = array('column' => 'status', 'value' => 1);

    unset($efq_params['bundle']);
  }

  $query = _totem_common_efq($efq_params);

  if (!empty($query->results)) {
    $subject = t('Recent @label', array('@label' => $type->name_plural));
    if (!empty($path)) {
      $subject = l($subject, $path);
    }

    $content = drupal_render($query->results);
    $content .= '<div class="clearfix"></div>';
    if (!empty($path)) {
      $content .= l(t('View All @label <span></span>', array('@label' => $type->name_plural)), $path, array('html' => TRUE, 'attributes' => array('class' => array('btn', 'corners', 'view-all'))));
    }

    $block = array(
      'subject' => $subject,
      'content' => array(
        '#type' => 'markup',
        '#markup' => $content,
        '#classes_array' => array('recent-type', 'recent-' . drupal_html_class($type->type)),
      ),
    );

    return $block;
  }
}
// TODO: Refactor this block to instead be _totem_common_block_view_active_type!!!
/**
 * Callback for "Active Communities" block view.
 */
function _totem_common_block_view_active_type($delta, $context) {

  $type = $context[0];

  // Get all the nids for communities.
  $nids = _totem_common_efq(array(
    'return' => 'entity_id',
    'entity_type' => 'node',
    'bundle' => $type,
  ))->results;

  // Loop through the nids, and grab the count of topics.
  // Force node table to be joined.
  $dis = "";
  foreach ($nids as $ix => $cid) {
    //Efq for active communities (this may change later).
    if (is_int($cid)) {
      $obj = _totem_common_efq(array(
        'entity_type' => 'node',
        'bundle' => 'topic',
        'count' => TRUE,
        'page_limit' => 5,
        'property_conditions' => array(
          array('column' => 'status', 'value' => 1),
        ),
        'field_conditions' => array(
          array('field' => 'field_community', 'column' => 'target_id', 'value' => $cid)
        ),
      ));

      $entities = $obj->results;
      $dis[$cid] = $entities;
    }
  }

  // Reverse sort, keeping nids as keys.
  arsort($dis);

  // Create the markup.
  $markup = "";

  // Render the nids.
  foreach ($dis as $key => $val) {
    $markup .= render(node_view(node_load($key), 'teaser'));
  }


  $block = array(
    'subject' => 'Most Active Communities',
    'content' => array(
      '#type' => 'markup',
      '#markup' => $markup,
    )
  );

  return $block;
}
/**
 * Callback for Featured Type list block view.
 */
function _totem_common_block_view_featured_type($delta, $context) {
  $type = $context[0];
  // Get featured items (args: FALSE theme'd returned data, TRUE limit results).
  $featured = totem_common_page_community_featured(FALSE, TRUE);
  unset($featured['teaser']->pager);
  //foreach featured full to get body array into teaser.
  foreach ($featured['teaser']->results as $id => $node) {
    $featured['teaser']->results[$id]['intro_text'] = $node['body'];
  }

  $block = array(
    'subject' => t('Featured @label', array('@label' => $type->name_plural)),
    'content' => array(
      '#type' => 'markup',
      '#markup' => drupal_render($featured['teaser']->results),
      '#classes_array' => array('featured-type', 'featured-' . drupal_html_class($type->type)),
    ),
  );

  return $block;
}
/**
 * Callback to list recent Resource types on the Resources Overview screen.
 */
function _totem_common_block_view_overview_resources($delta, $context) {

  $type = $context[0];

  $query = _totem_common_efq(array(
    'entity_type' => 'node',
    'bundle' => $type->type,
    'page_limit' => PAGE_SIZE_LISTS_TEASER,
    'property_order_by' => array(
      array('column' => 'changed', 'direction' => 'DESC'),
    ),
  ));

  $more_link = '<div class="clearfix"></div>' . l(t('View All @label <span></span>', array('@label' => $type->name_plural)), $type->path_plural, array('html' => TRUE, ));

  if (empty($query->results)) {
    // Force #theme to output block regardless of empty results.
    $query->results = array(
      '#type' => 'markup',
      '#markup' => variable_get('totem_common_no_results_text_default', '<p>No content available yet.</p>'),
    );

    $more_link = NULL;
  }

  $subject = l(t($type->name_plural), $type->path_plural);
  $block = array(
    'subject' => '<none>',
    'content' => array(
      '#classes_array' => array('box', 'box-' . drupal_html_class($type->type), 'clearfix'),
      '#theme' => 'totem_common_node_community_box',
      '#hook' => NULL,
      '#query' => $query,
      '#path' => $type->path_plural,
      '#header_block' => _totem_common_embed_block('totem_common', 'button_add_type', $type, 'Add'),
      '#title_link' => $subject,
      '#more_link' => $more_link,
      '#show_pager' => FALSE,
    ),
  );

  return $block;
}
/**
 * Shared data processor for all Community Overview blocks.
 */
function _totem_common_block_view_overview_community_stub($delta, $context) {

  $node = _totem_common_get_community_context_node();

  if (!empty($node)) {
    // Kind of a goofy check to force all overview blocks to appear exclusively
    // on the default tab; they should not appear on local task callbacks. This
    // saves us from doing extra PHP block visibility checking.
    if (drupal_get_path_alias("node/{$node->nid}") == drupal_get_path_alias()) {

      $module = $context[0];
      $type = $context[1];
      $hook_arguments = $context[2];
      $hook = "node_community_{$type}";
      $query = NULL;

      if (module_exists($module)) {
        $hook_args = array(
          'node' => $node,
          'page_limit' => PAGE_SIZE_LISTS_TEASER,
        );

        $hook_args = array_merge($hook_args, $hook_arguments);

        // Allow modules to alter params.
        // Note that other totem modules are already passing their specific
        // arguments via $context, so this is more for site-specific modules.
        $alter_type = __FUNCTION__;
        drupal_alter('totem_common_EFQ_params', $hook_args, $alter_type, $module);

        $items = module_invoke($module, $hook, $hook_args);
        $query = _totem_common_efq_extract_query($items);
      }

      // Note: several modules call block_view on this stub block and thus pass
      // any context params they wish directly. So could pass the path_plural
      // there (without having to find it), but that additional parameter
      // would be rather unclear in those calls.
      $types_info = _totem_common_types_info();
      $path = "node/{$node->nid}/{$types_info[$type]->path_plural}";

      $block = array(
        'subject' => t('Community Overview block stub'),
        'content' => array(
          '#classes_array' => array('box', 'box-' . drupal_html_class($type), (!empty($query->results) ? 'clearfix' : 'element-invisible')),
          '#theme' => 'totem_common_node_community_box',
          '#hook' => $hook,
          '#query' => $query,
          '#path' => $path,
        ),
      );

      return $block;
    }
  }
}
/**
 * Shared data processor for all Member Overview blocks.
 */
function _totem_common_block_view_overview_user_stub($delta, $context) {

  global $user;

  $account = NULL;

  // Try to load profile being viewed.
  $uid = arg(1);
  if (is_numeric($uid)) {
    $account = user_load($uid);
  }

  // Nothing loaded, default to current user.
  if (empty($account)) {
    $account = user_load($user->uid);
  }

  if (!empty($account)) {
    // Kind of a goofy check to force all overview blocks to appear exclusively
    // on the default tab; they should not appear on local task callbacks. This
    // saves us from doing extra PHP block visibility checking.
    if (drupal_get_path_alias("user/{$account->uid}") == drupal_get_path_alias()) {

      $module = $context[0];
      $type = $context[1];
      $hook_arguments = $context[2];
      $hook = "user_community_{$type}";
      $query = NULL;

      if (module_exists($module)) {
        $hook_args = array(
          'account' => $account,
          'page_limit' => PAGE_SIZE_LISTS_TEASER,
        );

        $hook_args = array_merge($hook_args, $hook_arguments);
        $items = module_invoke($module, $hook, $hook_args);
        $query = _totem_common_efq_extract_query($items);
      }

      // Note: several modules call block_view on this stub block and thus pass
      // any context params they wish directly. So could pass the path_plural
      // there (without having to find it), but that additional parameter
      // would be rather unclear in those calls.
      $types_info = _totem_common_types_info();
      $path = "user/{$account->uid}/{$types_info[$type]->path_plural}";

      $block = array(
        'subject' => t('Member Overview block stub'),
        'content' => array(
          '#classes_array' => array('box', 'box-' . drupal_html_class($type), (!empty($query->results) ? 'clearfix' : 'element-invisible')),
          '#theme' => 'totem_common_node_community_box',
          '#hook' => $hook,
          '#query' => $query,
          '#account' => $account,
          '#is_own_profile' => ($account->uid == $user->uid),
          '#path' => $path,
        ),
      );

      return $block;
    }
  }
}
/**
 * Callback for list block on Member Overview screen.
 */
function _totem_common_block_view_overview_user($delta, $context) {

  $block = module_invoke('totem_common', 'block_view', 'overview_user_stub', array(
    'totem_common',
    'community',
    array(
      'page_limit' => NULL,
    ),
  ));

  if (!empty($block)) {
    // Force this block to always show.
    unset($block['content']['#classes_array']);

    $types = _totem_common_node_types();

    $block['subject'] = t('Communities');
    $block['content'] = array_merge_recursive($block['content'], array(
      '#classes_array' => array('box', 'box-community', 'clearfix', 'communities'),
      '#header_block' => _totem_common_embed_block('totem_common', 'button_add_type', $types['community'], 'Add'),
      '#title_link' => $block['subject'],
      '#more_link' => NULL,
      '#show_pager' => FALSE,
    ));

    return $block;
  }
}
/**
 * Callback for list block on Member Overview screen.
 */
function _totem_common_block_view_overview_user_orphan($delta, $context) {

  $block = module_invoke('totem_common', 'block_view', 'overview_user_stub', array(
    'totem_common',
    'orphan',
    array(
      'page_limit' => NULL,
    ),
  ));

  if (!empty($block)) {
    $block['subject'] = l(t('Orphaned Content'), $block['content']['#path']);
    $block['content'] = array_merge_recursive($block['content'], array(
      '#classes_array' => array('communities'),
      '#header_block' => NULL,
      '#title_link' => $block['subject'],
      '#more_link' => '<div class="clearfix"></div>' . l(t('View All Orphaned Content <span></span>'), $block['content']['#path'], array('html' => TRUE, 'attributes' => array('class' => array('btn', 'corners')))),
      '#show_pager' => FALSE,
    ));

    return $block;
  }
}

/**
 * Callback for All/Private filters block view.
 *
 * @see totem_common_node_submit()
 * @see totem_common_block_view_filter_node_status()
 * @see totem_common_query_NODE_FILTER_alter()
 */
function _totem_common_block_view_filter_node_status($delta, $context) {

  global $user;

  if (user_access('view own unpublished content')) {

    $path = drupal_get_path_alias();
    $query = drupal_get_query_parameters();
    $filters = _totem_common_session_get('filter');

    if (array_key_exists('private_active', $query)) {
      if (!empty($query['private_active'])) {
        drupal_set_message(t('You are now viewing only your private content.<br />This filter will remain active until you click "All".'));
      }
      else {
        drupal_set_message(t('You are now viewing all content.'));
      }
    }

    $link_off = l(t('All'), "user/{$user->uid}/session", array(
      'query' => array(
        'destination' => url($path, array(
          'query' => array(
            // Append "filter applied" indicator so we can show a confirmation
            // message on redirect from user/uid/session.
            'private_active' => FALSE,
          ),
        )),
        'filter' => array(
          'node' => array(
            'status' => FALSE, // Negated in EFQ
          )
        ),
      ),
      'attributes' => array(
        'class' => 'all' . (empty($filters['node']['status']) ? ' active' : ''),
        'title' => t('Show all content.'),
      )
    ));

    $link_on = l(t('Private'), "user/{$user->uid}/session", array(
      'query' => array(
        'destination' => url($path, array(
          'query' => array(
            // Append "filter applied" indicator so we can show a confirmation
            // message on redirect from user/uid/session.
            'private_active' => TRUE,
          ),
        )),
        'filter' => array(
          'node' => array(
            'status' => TRUE, // Negated in EFQ
          )
        ),
      ),
      'attributes' => array(
        'class' => 'private' . (!empty($filters['node']['status']) ? ' active' : ''),
        'title' => t('Show only my Private content.'),
      )
    ));


    $block = array(
      'subject' => t(''),
      'content' => array(
        '#type' => 'markup',
        '#markup' => '<div class="filter-private">' . $link_off . ' ' . $link_on . '</div>',
      ),
    );

    return $block;
  }
}
