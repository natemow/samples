<?php
/**
 * @file
 * totem_common.pages.inc
 */

/**
 * TODO.
 */
function totem_common_page_user_session($account) {

  $args = drupal_get_query_parameters($_GET, array('q', 'destination'));

  foreach ($args as $key => $val) {
    _totem_common_session_set($key, $val);
  }

  $destination = drupal_get_destination();
  drupal_goto($destination);
}
/**
 * TODO.
 */
function totem_common_page_autocomplete_taxonomy($bundle, $string) {

  module_load_include('inc', 'taxonomy', 'taxonomy.pages');

  $entity_type = 'node';
  if ($bundle == 'user') {
    $entity_type  = 'user';
  }

  $json_strings = '';

  // Get all fields for this bundle type.
  $fields = field_info_instances($entity_type, $bundle);
  if (!empty($fields)) {

    // Turn on output buffering to catch taxonomy_autocomplete's echo of JSON.
    ob_start();
    foreach ($fields as $key => $meta) {
      // Make sure this is a taxonomy field.
      if (!empty($meta['widget']['module']) && $meta['widget']['module'] == 'taxonomy') {

        // Get term matches for this field and add to results.
        taxonomy_autocomplete($meta['field_name'], $string);
        $json = ob_get_contents();
        // Do some ugly stuff to build the outer string results var.
        $json = str_replace('}{', '},{', $json);
        $json = str_replace('[]', '', $json);
        $json_strings .= $json . ',';

      }
    }
    // Turn off output buffering.
    ob_end_clean();

    $json_strings = trim($json_strings, ',');
  }

  // Now output a single array of unique term results.
  $results = array();
  if (!empty($json_strings)) {
    $json_strings = drupal_json_decode('[' . $json_strings . ']');
    foreach ($json_strings as $group) {
      $results = array_merge($results, $group);
    }
  }

  drupal_json_output($results);
}
/*
 * TODO: make this less sloppy? It's not a great solution but works for now.
 */
function totem_common_rss_generator($args) {

  // Create RSS Link.
  $thelink = 'rss/';

  // There are some pages we just have to one off, so deal with that now.
  switch ($args[0]) {
    // Users don't have RSS feeds for now, so return false.
    case 'members':
    case 'user':
      return FALSE;
      break;

    case 'node':
      if (end($args) == 'members') {
        return FALSE;
      }
      break;

    case 'resources':
      // The resources list page does not have a rss link right now.
      // But this is what we would put: $thelink .= 'resources';
      break;

  }

  // Get Types info.
  $types = _totem_common_types_info();
  foreach ($types as $type) {
    // We're going to build based on a the last item in args.
    if (end($args) == end(explode('/', $type->path_plural))) {
      $thelink .= str_replace('_', '-', $type->type);
    }
  }

  $node = _totem_common_get_community_context_node();
  // Is it a community centric page.
  if (!empty($node)) {
    $cid = $node->nid;
    // Are we on the community overview page?
    if (end($args) == $cid) {
      $thelink .= 'recent';
    }
    $thelink .= '/' . $cid;
  }

  return $thelink;
}
/*
 * Create and return RSS feed for bundle and cid
 */
function totem_common_rss_feeder($bundle, $cid = NULL) {

  $bundle = str_replace('-', '_', $bundle);
  $base = array(
    'return' => 'entity_id',
    'entity_type' => 'node',
    'bundle' => $bundle,
    'page_limit' => NULL,
    'property_order_by' => array(
      array('column' => 'changed', 'direction' => 'DESC'),
    ),
  );

  if (!empty($cid)) {
    $base['field_conditions'][] = array(
      'field' => 'field_community',
      'column' => 'target_id',
      'value' => $cid,
    );
  }

  $nids = array();
  switch ($bundle) {
    case 'resources':
      // Get the resource machine names.
      $base['bundle'] = array_keys(_totem_common_node_types(TRUE));
      $nids = _totem_common_efq($base)->results;
      break;

    case 'recent':
      $vars['return'] = 'entity_id';
      $vars['page_limit'] = NULL;
      $vars['node'] = node_load($cid);

      $recent = module_invoke_all('node_community_recent', $vars);
      if (!empty($recent)) {
        $nids = $recent['entities']['query']->results;
      }
      break;

    default:
      $nids = _totem_common_efq($base)->results;
      break;
  }

  $channel = array();

  node_feed($nids, $channel);
}

/**
 * Page callback for top-level Featured Communities screen.
 */
function totem_common_page_community_featured($themed = TRUE, $limit = FALSE) {

  $efq_params = array(
    'entity_type' => 'node',
    'bundle' => 'community',
    'property_conditions' => array(
      array('column' => 'status', 'value' => 1),
    ),
    'property_order_by' => array(
      array('column' => 'changed', 'direction' => 'DESC'),
    ),
    'field_conditions' => array(
      array('field' => 'field_community_featured', 'column' => 'value', 'value' => 1),
    ),
    // Need to run this query without any node_access checking in
    // place...otherwise, anon users, etc. may not see all the Featured nodes.
    // @see http://drupal.org/node/1597378, http://drupal.org/node/1440976, http://api.drupal.org/api/drupal/modules!field!modules!field_sql_storage!field_sql_storage.module/function/field_sql_storage_field_storage_query/7
    'tags' => array('DANGEROUS_ACCESS_CHECK_OPT_OUT'),
  );

  if ($limit) {
    $efq_params['page_limit'] = PAGE_SIZE_LISTS_TEASER;
  }

  $query_teaser = _totem_common_efq($efq_params);

  $efq_params['view_mode'] = 'full';
  $query_full = _totem_common_efq($efq_params);

  // Consolidate entity URL params here so we don't clutter up our .tpl.
  foreach ($query_full->results as &$entity) {

    $url = url("node/{$entity['#node']->nid}", array(
      'entity_type' => 'node',
      'entity' => $entity['#node'],
      'absolute' => TRUE,
    ));

    $entity['link_title'] = l($entity['#node']->title, $url);
    $entity['link_image'] = NULL;
    if (!empty($entity['field_image'][0]['sized_images'])) {
      $entity['link_image'] = l($entity['field_image'][0]['sized_images']['community_image'], $url, array(
        'html' => TRUE,
      ));
    }

    $entity['link_button'] = l(t('View @community', array('@community' => t('Community'))), $url, array(
      'attributes' => array(
        'class' => array('btn', 'small', 'corners'),
      ))
    );
  }

  if ($themed) {
    $return_data = theme('totem_common_page_community_featured', array(
      'query_teaser' => $query_teaser,
      'query_full' => $query_full,
    ));
  }
  else {
    $return_data = array('teaser' => $query_teaser, 'full' => $query_full);
  }
  return $return_data;
}
/**
 * Page callback for top-level Resources overview screen.
 */
function totem_common_page_resources() {

  // Get all Resources types, output list block per.
  $types = _totem_common_node_types(TRUE);
  $content = array();
  foreach ($types as $type) {
    $content[] = _totem_common_embed_block('totem_common', 'overview_resources', $type);
  }

  return $content;
}
/**
 * Page callback for top-level content listing of core platform types.
 *
 * @param object $type_info An object of node type info or similar info for
 * other pseudo-types, as returned by _totem_common_types_info().
 *
 * @return array Render array with theme hook 'totem_common_paged_entities' by default.
 */
function totem_common_page_content_list($type_info) {
  // For most types, add rss feed to header.
  if (!in_array($type_info->type, array('user'))) {
    drupal_add_feed('rss/' . $type_info->type, 'List of ' . $type_info->name_plural);
  }

  // Base efq array for list.
  // Force entity_type table to be joined.
  $efq_params = array(
    'entity_type' => 'node',
    'bundle' => $type_info->type,
    'page_limit' => PAGE_SIZE_LISTS_PAGE,
    'property_conditions' => array(
      array('column' => 'status', 'value' => 1),
    ),
    'property_order_by' => array(
      array('column' => 'changed', 'direction' => 'DESC'),
    ),
  );

  if ($efq_params['bundle'] == 'user') {
    $efq_params['entity_type'] = 'user';
    $efq_params['property_order_by'][0]['column'] = 'created';
  }

  // Allow modules to alter params.
  $efq_param_type = 'search';
  drupal_alter('totem_common_EFQ_params', $efq_params, $efq_param_type);

  // Alter params for this page callback specifically.
  $efq_param_type = 'content_list';
  drupal_alter('totem_common_EFQ_params', $efq_params, $efq_param_type);


  $build = array(
    '#theme' => 'totem_common_paged_entities',
    '#query' => _totem_common_efq($efq_params),
  );

  // Allow modules to alter render array for this particular type.
  drupal_alter('totem_common_page_content_list_' . $type_info->type, $build);

  drupal_set_title($type_info->name_plural);

  return $build;
}
/**
 * Implements hook_totem_common_page_content_list_TYPE_alter() for 'community'.
 * This replaces the rest of the old callback totem_common_page_community().
 */
function totem_common_totem_common_page_content_list_community_alter(&$build) {

  // Build common parameters for count queries.
  // Note: return parameter doesn't matter because count = TRUE
  // forces skipping all processing of results.
  $efq_params_count = array(
    'property_conditions' => array(
      array('column' => 'status', 'value' => 1),
    ),
    'field_conditions' => array(
      array('field' => 'field_community', 'column' => 'target_id', 'value' => 0),
    ),
    'count' => TRUE,
  );

  // Build specific count queries.
  $efqs_count = array();

  $efqs_count['members'] = $efq_params_count;
  $efqs_count['members']['entity_type'] = 'user';

  $efqs_count['media'] = $efq_params_count;
  $efqs_count['media']['entity_type'] = 'node';
  $efqs_count['media']['bundle'] = 'media';

  $efqs_count['discussions'] = $efq_params_count;
  $efqs_count['discussions']['entity_type'] = 'node';
  $efqs_count['discussions']['bundle'] = 'topic';

  $counts = array();

  foreach ($build['#query']->results as $cnid => &$cnode_build) {
    foreach ($efqs_count as $data_type => $efq_params) {
      // Find all entities in the community.
      $efq_params['field_conditions'][0]['value'] = $cnid;
      // Store the count.
      $counts[$cnid][$data_type] = _totem_common_efq($efq_params)->results;

      // Attach node URL to its render array, so we can access it in template
      // for the "More details" link.
      $cnode_uri = entity_uri('node', $cnode_build['#node']);
      $cnode_build['#node_url'] = url($cnode_uri['path'], $cnode_uri['options']);

      // Modal processing for URL attributes.
      // @see totem_common_preprocess_node()
      $node_url_attributes = array(
        'title' => t("More details"),
        'class' => array('btn', 'more'),
      );
      _totem_common_modal_link_attributes_ensure($cnode_build['#node_url'], $node_url_attributes);
      $cnode_build['#node_url_attributes'] = $node_url_attributes;
    }
  }

  // Use custom theme hook for totem-common-page-communities.tpl.php.
  $build['#theme'] = 'totem_common_page_communities';
  $build['#counts'] = $counts;
}
/**
 *
 * @param type $node
 * @param type $type
 * @param type $title
 * @return type
 */
function totem_common_node_community($node, $type, $title, $ajax) {

  $hook = "node_community_{$type}";
  $entity_id = NULL;
  $hook_arguments = array(
    'node' => $node,
    'page_limit' => 1,
    'view_mode' => 'full',
  );

  // Use first numeric arg as entity_id.
  $args = func_get_args();
  foreach ($args as $arg) {
    if (is_numeric($arg)) {
      $entity_id = $arg;
      break;
    }
  }

  if (!empty($entity_id)) {
    $hook .= "_entity";
    $hook_arguments['entity_id'] = $entity_id;
  }

  $items = module_invoke_all($hook, $hook_arguments);
  $query = _totem_common_efq_extract_query($items);
  $build = array(
    '#theme' => 'totem_common_node_community_tab',
    '#type' => $type,
    '#title' => t($title),
    '#content' => NULL,
  );

  // Theme results.
  if (!empty($query)) {
    if (count($query->results) == 1) {
      $build['#content'] = &array_pop($query->results);
    }
    elseif (count($query->results) > 0) {
      $build['#content'] = array(
        '#theme' => 'totem_common_paged_entities',
        '#query' => $query,
      );
    }
  }

  // Set empty resultset message.
  if (empty($build['#content'])) {
    $types = _totem_common_types_info();
    $filters = _totem_common_session_get('filter');

    $build['#content'] = array(
      '#type' => 'markup',
      '#markup' => $types[$type]->no_results_text_community,
    );

    // Alter message if "Private" filter is active.
    if (!empty($filters['node']['status'])) {
      $build['#content']['#markup'] = $types[$type]->no_results_text_community_private_filter_active;
    }
  }

  // Allow modules to alter output.
  drupal_alter($hook, $build);

  // Return AJAX commands if mode specified.
  // @see totem_common.js()
  if ($ajax) {
    $content = drupal_render($build);

    // Ugly hack to strip /ajax/ from content hrefs. This fixes
    // ?destination=$path1 and redirects user to non-AJAX menu path.
    $path1 = drupal_get_path_alias();
    $path2 = str_replace('/ajax/', '/', $path1);
    $content = str_replace($path1, $path2, $content);

    $build = array(
      '#type' => 'ajax',
      '#commands' => array(
        // Invoke $.fn.ajaxContentEffect.
        ajax_command_invoke(NULL, 'ajaxContentEffect', array(
          url($path2),
          $content,
        )),
      ),
    );
  }

  return $build;
}
