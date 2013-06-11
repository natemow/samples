<?php
/**
 * @file
 * totem_common.form.inc
 */

/**
 * TODO.
 */
function _totem_common_form_resoure_bool($node_type) {
  // Core types cannot be resources.
  if (strpos($node_type->module, 'totem_') === 0) {
    return FALSE;
  }
}

// Hook implementations.
/**
 * Implements hook_form_alter().
 * PERMISSION
 */
function totem_common_form_alter(&$form, &$form_state, $form_id) {

  // Add attributes to action buttons.
  if (!empty($form['actions'])) {

    foreach ($form['actions'] as $key => &$meta) {
      if (is_array($meta) && !empty($meta['#type']) && in_array($meta['#type'], array('submit', 'button'))) {
        $meta['#attributes']['class'][] = 'btn';
        $meta['#attributes']['class'][] = 'corners';
      }
    }

    // Apply clearfix to actions wrapper (instead of adding an empty clearfix
    // div as #suffix, which was creating unnecessary scrollbar on modals).
    $form['actions']['#attributes']['class'][] = 'clearfix';

    // Because so many forms are loaded by AJAX
    // and some have AJAX handling attached to their submit button as well,
    // make a more unique ID for submit buttons to avoid clashes
    // in the javascript Drupal.ajax array, which is keyed
    // by HTML id attributes (except those set up by ctools).
    if (!empty($form['actions']['submit'])) {
      // Create an id attribute similar to the default, as generated in form_builder().
      $form['actions']['submit']['#id'] = drupal_html_id('edit-submit--' . str_replace('_form', '', $form_id));
    }

    if (!empty($form['actions']['cancel'])) {
      $form['actions']['cancel']['#attributes']['class'][] = 'cancel';
    }
  }

  // THIS IS SUPER-IMPORTANT!!!!!!!
  /*
   * If the auto-complete field_community entityreference field isn't completely
   * removed from all forms and managed EXCLUSIVELY in code, all kinds of
   * malicious hacks are possible via core paths.
   *
   * E.g.:
   *  - node/add/topic/[not-your-community-nid]
   *  - user/2/edit
   *
   * Checks on:
   *  - community/add/topic/[not-your-community-nid]
   *
   * ...are well-accounted for in
   * @see totem_user_node_access()
   *
   *
   * Use $form['field_community_context'] if you need to POST
   * hidden input or something.
   *
   */

  if (!empty($form['field_community'])) {
    $form['field_community']['#access'] = user_access('administer nodes');
  }
  if (!empty($form['field_community_featured'])) {
    $form['field_community_featured']['#access'] = user_access('administer nodes');
  }
  if (!empty($form['field_community_blocked'])) {
    $form['field_community_blocked']['#access'] = user_access('administer users');
  }
  if (!empty($form['field_community_pending'])) {
    $form['field_community_pending']['#access'] = user_access('administer users');
  }

  // Do some translation stuff on descriptions.
  $elements = _totem_common_get_form_elements($form);
  foreach ($elements as $key => &$meta) {
    if (!empty($form[$key]['#language'])) {
      if (!empty($form[$key][$form[$key]['#language']]['#description'])) {
        $form[$key][$form[$key]['#language']]['#description'] = t(str_ireplace('Community', '@community', $form[$key][$form[$key]['#language']]['#description']), array('@community' => t('Community')));
      }
    }
  }

  // Pretty up confirm forms to avoid extra styling.
  // @see confirm_form()
  if (isset($form['#theme']) && $form['#theme'] == 'confirm_form') {
    if (isset($form['description']) && !empty($form['description']['#markup'])) {
      $form['description']['#prefix'] = '<p class="description">';
      $form['description']['#suffix'] = '</p>';
    }
  }

}

// Admin forms.
/**
 * Form for admin settings.
 */
function totem_common_form_settings($form, &$form_state) {

  /*
  $form['global_config'] = array(
    '#type' => 'fieldset',
    '#title' => t('Common settings'),
    '#description' => t('Set global parameters for the @site_name site.', array('@site_name' => check_plain(variable_get('site_name')))),
    '#collapsible' => TRUE,
    '#collapsed' => FALSE,
  );
  */

  $form = system_settings_form($form);

  return $form;
}
/**
 * Implements hook_form_FORM_ID_alter().
 */
function totem_common_form_system_performance_settings_alter(&$form, &$form_state, $form_id) {
  $form['#submit'][] = 'totem_common_form_system_performance_settings_submit';
}
/**
 * Custom submit handler for system_performance_settings form.
 */
function totem_common_form_system_performance_settings_submit($form, &$form_state) {
  // Clear APC cache.
  $apc_enabled = ini_get('apc.enabled');
  if (!empty($apc_enabled) && function_exists('apc_clear_cache')) {
    apc_clear_cache();
    watchdog(variable_get('install_profile'), 'Cleared APC opcode cache.');
  }
}
/**
 * Implements hook_form_FORM_ID_alter().
 *
 * We don't want admins screwing around with the permissions for nodes or
 * internal_nodes; if they did, a lot of the field_community logic defined in
 * our hook_node_access implementations would break or be nullified.
 *
 * @see totem_user_node_access()
 */
function totem_common_form_user_admin_permissions_alter(&$form, &$form_state, $form_id) {

  $disabled_perms = array();

  // Node module perms.
  foreach (node_permissions_get_configured_types() as $type) {
    $disabled_perms += node_list_permissions($type);
  }

  // Internal_nodes module perms.
  $types = node_type_get_types();
  $names = node_type_get_names();
  foreach ($names as $key => $name) {
    $type = $types[$key];
    $url = 'admin/structure/types/manage/' . str_replace('_', '-', $key);
    $options['fragment'] = 'edit-internal-nodes';
    $disabled_perms['access ' . $key . ' node view'] = array(
      'title' => t('Access !name node view', array('!name' => l($name, $url, $options))),
    );
  }

  $disabled_perms = array_keys($disabled_perms);


  // Disable the checkbox for each permission for each role.
  foreach (element_children($form['checkboxes']) as $key) {
    foreach ($disabled_perms as $perm) {
      // TODO: Disabling actually removes the perm from role_permissions table completely?!
      // $form['checkboxes'][$key][$perm]['#disabled'] = TRUE;
    }
  }

  // Per TODO above, add a big fat warning message for the $disabled_perms instead.
  $hide_descriptions = system_admin_compact_mode();
  $module_info = system_get_info('module');
  $modules = array();
  foreach (module_implements('permission') as $module) {
    $modules[$module] = $module_info[$module]['name'];
  }
  asort($modules);

  foreach ($modules as $module => $display_name) {
    if ($permissions = module_invoke($module, 'permission')) {
      foreach ($permissions as $perm => $perm_item) {
        if (in_array($perm, $disabled_perms)) {
          $perm_item['restrict access'] = TRUE;
          $perm_item['warning'] = t('Warning: Give to trusted roles only; this permission has security implications. Changing<br />the default values may break or compromise the @site_name installation profile.', array('@site_name' => variable_get('site_name')));
          $form['permission'][$perm]['#description'] = theme('user_permission_description', array('permission_item' => $perm_item, 'hide' => $hide_descriptions));
        }
      }
    }
  }

}
/**
 * Implements hook_form_FORM_ID_alter().
 *
 * adding a vertical tab for resource checkbox
 *
 */
function totem_common_form_node_type_form_alter(&$form, $form_state) {
  // Grab the value of the resource variable (if it exists).
  if ($form['type']['#default_value']) {
    $resval = variable_get("totem_common_resource_{$form['type']['#default_value']}");
  }
  else {
    $resval = 0;
  }

  $form['totem_config'] = array(
    '#type' => 'fieldset',
    '#title' => t('@site_name configuration', array('@site_name' => check_plain(variable_get('site_name')))),
    '#collapsible' => TRUE,
    '#collapsed' => TRUE,
    '#group' => 'additional_settings',
    '#attributes' => array('class' => array('resource-form')),
    '#weight' => 51
  );

  $form['totem_config']['resource_box'] = array(
    '#type' => 'checkbox',
    '#title' => t('Resource'),
    '#description' => t('Is this content type a resource?'),
    '#default_value' => $resval,
    '#access' => _totem_common_form_resoure_bool($form['#node_type']),
  );

  $form['totem_config']['no_results_text_community'] = array(
    '#type' => 'textarea',
    '#title' => t("@community - &quot;No nodes available&quot; text", array('@community' => t('Community'))),
    '#description' => t('Define the text that will show up when no content is available for this type in a @community.', array('@community' => t('Community'))),
    '#default_value' => variable_get("totem_common_no_results_text_community_{$form['type']['#default_value']}", ''),
  );

  $form['totem_config']['no_results_text_community_private_filter_active'] = array(
    '#type' => 'textarea',
    '#title' => t("@community - &quot;No nodes available&quot; text (Private content only)", array('@community' => t('Community'))),
    '#description' => t('Define the text that will show up when no content is available for this type in a @community, and when the user has the "Private" content filter enabled..', array('@community' => t('Community'))),
    '#default_value' => variable_get("totem_common_no_results_text_community_private_filter_active_{$form['type']['#default_value']}", ''),
  );

  $form['totem_config']['no_results_text_user'] = array(
    '#type' => 'textarea',
    '#title' => t("Member profile - &quot;No nodes available&quot; text"),
    '#description' => t('Define the text that will show up when no content is available for this type on a member profile.'),
    '#default_value' => variable_get("totem_common_no_results_text_user_{$form['type']['#default_value']}", ''),
  );

  // Add custom submit handler.
  $form['#submit'][] = 'totem_common_form_node_type_form_submit';

  return $form;
}
/**
 * TODO.
 */
function totem_common_form_node_type_form_submit($form, &$form_state) {

  $type = $form_state['values']['type'];

  // Set variables for "No data available" for this type.
  variable_set("totem_common_no_results_text_community_{$type}", $form_state['values']['no_results_text_community']);
  variable_set("totem_common_no_results_text_community_private_filter_active_{$type}", $form_state['values']['no_results_text_community_private_filter_active']);
  variable_set("totem_common_no_results_text_user_{$type}", $form_state['values']['no_results_text_user']);

  // Set variables and perms for this "Resources" type.
  if ($form_state['values']['resource_box'] == 1) {
    // Flag this content type as a resource.
    variable_set("totem_common_resource_{$type}", TRUE);

    // Rebuild Resources menu items.
    _totem_common_set_resources_menu_items();

    // Set new permissions for type.
    user_role_grant_permissions(DRUPAL_AUTHENTICATED_RID, array(
      "create {$type} content",
      "edit own {$type} content",
      "delete own {$type} content",
    ));

    // Update "Moderator" perms.
    user_role_grant_permissions(4, array(
      "edit any {$type} content",
    ));

    // Update "Administrator" perms.
    user_role_grant_permissions(3, array_keys(module_invoke_all('permission')));
  }
  else {
    totem_common_form_node_type_delete_confirm_submit($form, $form_state);
  }

  // Flush all caches.
  drupal_flush_all_caches();
}
/**
 * Implements hook_form_FORM_ID_alter().
 */
function totem_common_form_node_type_delete_confirm_alter(&$form, $form_state) {

  // Add custom submit handler.
  $form['#submit'][] = 'totem_common_form_node_type_delete_confirm_submit';

}
/**
 * TODO.
 */
function totem_common_form_node_type_delete_confirm_submit($form, &$form_state) {

  // Get "Resources" types.
  $type = $form_state['values']['type'];
  $types = _totem_common_node_types(TRUE);

  // Clean up variable and make sure we don't wind up with orphan menu items.
  if (!empty($types)) {
    variable_del("totem_common_resource_{$type}");
    menu_link_delete(NULL, $types[$type]->path_plural);

    // Now rebuild Resources menu items.
    _totem_common_set_resources_menu_items();
  }

}

// Node + custom forms.
/**
 * Modal node add/edit/delete/remove forms.
 */
function totem_common_form_node($op, $node, $node_parent) {

  global $user;

  $type = (is_string($node) ? $node : $node->type);
  $verb =
  $title = NULL;
  $form_id = $type . '_node_form';

  switch ($op) {
    case 'edit':
      $verb = 'Edit';
      break;

    case 'delete':
      $verb = 'Delete';
      $form_id = 'node_delete_confirm';
      break;

    case 'add':
      $verb = 'Add';
      $node = (object) array(
        'uid' => $user->uid,
        'name' => (isset($user->name) ? $user->name : ''),
        'type' => $type,
        'status' => NODE_PUBLISHED,
        'language' => LANGUAGE_NONE,
      );


      $resources = array_keys(_totem_common_node_types(TRUE));
      if (!in_array($node->type, $resources)) {
        // Auto-assign this node's field_community value based on $parent_node arg.
        // Check if $node_parent arg was passed, in case $node is being added
        // outside of a community context (e.g. resource types).
        if (!empty($node_parent) && $node->type !== 'community' && $node_parent->type == 'community') {
          $node->field_community[$node->language][0]['target_id'] = $node_parent->nid;
        }
      }

      break;

    case 'remove':
      $verb = 'Remove';
      $form_id = 'totem_common_form_node_remove';
      break;
  }


  // For non-AJAX debug:
  // $form = drupal_get_form($form_id, $node); return $form;.
  if (empty($title)) {
    $types = _totem_common_node_types();
    $title = t('@verb @name', array('@verb' => t($verb), '@name' => t($types[$node->type]->name)));
  }
  else {
    $title = t($title);
  }


  $form_state = array(
    'title' => $title,
    'ajax' => TRUE,
    'build_info' => array(
      'args' => array($node, $node_parent),
      // Add a custom flag to indicate from where the form was called.
      // This allows form alters to depend on $form_state['build_info']['args'].
      'generator' => __FUNCTION__,
    ),
  );

  form_load_include($form_state, 'inc', 'node', 'node.pages');
  ctools_include('modal');
  ctools_include('ajax');


  $output = ctools_modal_form_wrapper($form_id, $form_state);

  if (!empty($form_state['executed'])) {
    ctools_add_js('ajax-responder');

    // Overwrite the form output.
    $output = array();

    // Load full node.
    $node = node_load($form_state['values']['nid']);

    // Make sure we've always got something in place for redirect.
    if (empty($form_state['redirect'])) {
      $form_state['redirect'] == "node/{$node->nid}";
    }

    if (!empty($form_state['redirect'])) {
      // Make sure we always stay in current community context by forcing
      // url() calls on $form_state['redirect'].
      if ($form_state['redirect'] == "node/{$node->nid}") {
        $form_state['redirect'] = ltrim(url($form_state['redirect'], array(
          'entity_type' => 'node',
          'entity' => $node,
          'entity_context_community' => $node_parent,
        )), '/');
      }

      $redirect = $form_state['redirect'];
    }

    if (!empty($redirect)) {

      // Usability improvement: forcefully disable "Private" session filter
      // if user just acted on a published node.
      if (!empty($node->status)) {
        $output[] = ctools_ajax_command_redirect("user/{$user->uid}/session", 0, array(
          'query' => array(
            'destination' => $redirect,
            'filter' => array(
              'node' => array(
                'status' => FALSE,
              ),
          )
        )));

        print ajax_render($output);
        exit;
      }

      // Redirect to form-defined path.
      $output[] = ctools_ajax_command_redirect($redirect);
    }
    else {
      // Refresh the page we came from.
      $output[] = ctools_ajax_command_reload();
    }
  }

  print ajax_render($output);
  exit;
}
/**
 * Implements hook_form_BASE_FORM_ID_alter().
 */
function totem_common_form_node_form_alter(&$form, &$form_state, $form_id) {

  // Add class to indicate type is a Resource.
  $types = _totem_common_node_types(FALSE);
  if ($types[$form['#node']->type]->is_resource) {
    $form['#attributes']['class'][] = 'resource';
  }

  // Add custom node.status override field.
  // @see totem_common_node_submit(), totem_common_block_view_filter_node_status(), totem_common_query_NODE_FILTER_alter()
  $allow_status = FALSE;
  $types = _totem_common_node_types();
  if (!empty($types)) {
    foreach ($types as $type) {
      if ($type->type == $form_state['node']->type) {
        if (!in_array($type->type, array('page', 'community'))) {
          $allow_status = TRUE;
        }
      }
    }
  }

  if ($allow_status) {
    // Default to checked if session filter is active.
    $filters = _totem_common_session_get('filter');
    if (!empty($filters['node']['status']) && empty($form_state['node']->nid)) {
      $form_state['node']->status = 0;
    }

    $form['status_override'] = array(
      '#access' => user_access('view own unpublished content'),
      '#type' => 'checkbox',
      '#title' => t('Make this item private? Only you will be able to view and manage private content.'),
      '#default_value' => !$form_state['node']->status,
    );
  }

  // Remove "Preview" and "Delete" buttons globally;
  // these functions will be handled elsewhere.
  unset($form['actions']['preview']);
  unset($form['actions']['delete']);

  // Attach a cancel link that (usually) behaves like modal close link.
  // @see confirm_form(), Drupal.behaviors.totem_common.modalAdjustments()
  $dest = drupal_get_destination();
  $dest = drupal_parse_url($dest['destination']);

  $form['actions']['cancel'] = array(
    '#type' => 'link',
    '#title' => t("Cancel"),
    '#href' => $dest['path'],
    '#options' => $dest,
    '#attributes' => array(
      'class' => array(
        'cancel',
      ),
    ),
  );

}
/**
 * Implements hook_node_submit().
 */
function totem_common_node_submit($node, $form, &$form_state) {

  // For checkbox definition.
  // @see totem_common_form_alter()
  if (!empty($form['status_override'])) {
    $node->status = !$form_state['values']['status_override'];
  }

}
/**
 * Implements hook_form_FORM_ID_alter().
 */
function totem_common_form_node_delete_confirm_alter(&$form, &$form_state, $form_id) {

  $node = $form_state['build_info']['args'][0];

  $form['#submit'][] = 'totem_common_form_node_delete_confirm_submit';

  $form['description']['#markup'] = t('Are you sure you want to delete %title and remove it from all @communities? This action cannot be undone.', array('%title' => $node->title, '@communities' => t('Communities')));

}
/**
 * Custom submit handler for node deletion.
 */
function totem_common_form_node_delete_confirm_submit($form, &$form_state) {

  // If deleting a node, force user back to default local task path (vs.
  // default node/nid_community/task/nid path).
  $path = drupal_get_destination();
  $form_state['redirect'] = str_ireplace("/{$form_state['values']['nid']}", '', $path['destination']);

}
/**
 * Menu callback -- ask for confirmation of node removal.
 */
function totem_common_form_node_remove($form, &$form_state) {

  $node = $form_state['build_info']['args'][0];
  $node_community = $form_state['build_info']['args'][1];

  // Build form.
  $form = array(
    '#submit' => array('totem_common_form_node_remove_submit'),
    '#node' => $node,
  );

  // Always provide entity id in the same form key as in the entity edit form.
  $form['nid'] = array(
    '#type' => 'value',
    '#value' => $node->nid,
  );

  // Get contextual community node.
  if ($node_community && $node_community->type == 'community') {
    $form['field_community_context'] = array(
      '#type' => 'hidden',
      '#value' => $node_community->nid,
    );
  }

  return confirm_form($form,
    t('Are you sure you want to remove %title?', array('%title' => $node->title)),
    'node/' . $node->nid,
    t('Are you sure you want to remove %title from %community? This action cannot be undone.', array('%title' => $node->title, '%community' => $node_community->title)),
    t('Remove'),
    t('Cancel')
  );
}
/**
 * Execute node removal from community.
 */
function totem_common_form_node_remove_submit($form, &$form_state) {
  if ($form_state['values']['confirm']) {
    if ($form_state['values']['field_community_context']) {

      // Remove community nid from node's field_community LOV.
      $node = $form['#node'];
      _totem_common_set_field_entityreference_values('node', $node, 'field_community', array($form_state['values']['field_community_context']), TRUE);
      drupal_set_message(t('@type %title has been removed.', array('@type' => node_type_get_name($node), '%title' => $node->title)));

      // If removing a node, force user back to default local task path (vs.
      // default node/nid_community/task/nid path).
      $path = drupal_get_destination();
      $form_state['redirect'] = str_ireplace("/{$form_state['values']['nid']}", '', $path['destination']);
    }
  }

}
