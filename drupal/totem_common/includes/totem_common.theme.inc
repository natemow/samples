<?php
/**
 * @file
 * totem_common.theme.inc
 */

/**
 * Implements MODULE_process().
 */
function totem_common_process(&$vars, $hook) {

  global $theme;
  if ($theme == variable_get('admin_theme')) {
    return;
  }

  // Add a bunch of custom theme suggestions.

  if (empty($vars['theme_hook_suggestions'])) {
    $vars['theme_hook_suggestions'][] = $hook;
  }

  // Add node type page suggestions.
  if ($hook == 'page') {
    if (!empty($vars['node'])) {
      $vars['theme_hook_suggestions'][] = 'page__' . $vars['node']->type;
    }
  }

  // Add "--totem" suggestions...these templates will be registered for
  // modules that correctly implement hook_theme_registry_alter.
  /*
   * @see totem_common_theme_registry_alter()
   */
  $theme_hook_suggestions_modules = array();
  foreach ($vars['theme_hook_suggestions'] as $template) {
    $theme_hook_suggestions_modules[] = $template . '__totem';
  }

  $vars['theme_hook_suggestions'] = array_merge($vars['theme_hook_suggestions'], $theme_hook_suggestions_modules);

  // Add resource tpl to theme suggestions.
  if ($hook == 'node') {
    $resources = array_keys(_totem_common_node_types(TRUE));
    if (in_array($vars['node']->type, $resources)) {
      $vars['theme_hook_suggestions'][] = 'node__resource';
    }
  }

}

/**
 * Implements MODULE_preprocess().
 */
function totem_common_preprocess(&$vars, $hook) {

  // Move contextual links from suffix to prefix.
  if (!empty($vars['title_suffix']['contextual_links'])) {
    $vars['title_prefix']['contextual_links'] = $vars['title_suffix']['contextual_links'];
    unset($vars['title_suffix']['contextual_links']);
  }

  // If set, merge in custom #classes_array, which can be set in the renderable
  // array at any point before theme() is called.
  if (!empty($vars['elements']['#classes_array'])) {
    foreach ($vars['elements']['#classes_array'] as &$class) {
      $class = drupal_html_class($class);
    }
    $vars['classes_array'] = array_merge($vars['classes_array'], $vars['elements']['#classes_array']);
  }

}
/**
 * Implements MODULE_preprocess_page().
 */
function totem_common_preprocess_page(&$vars) {

  $vars['is_admin'] = !stristr(path_to_theme(), variable_get('theme_default'));
  $vars['tab_utility'] = array();

  if ($vars['is_admin']) {
    return;
  }

  // Add module UI assets.
  $path = drupal_get_path('module', 'totem_common');

  drupal_add_css($path . '/theme/totem_common.css');

  // TODO: convert these into simple calls to libraries_load(), which does
  // all necessary drupal_add_js()/drupal_add_css() calls...
  // history.js is a working example.
  // @see totem_common_libraries_info()

  $library = libraries_load('jquery.tooltip');
  drupal_add_js($library['library path'] . '/jquery.tooltip.min.js', array(
    'scope' => 'footer',
  ));

  $library = libraries_load('jquery.jscrollpane');
  drupal_add_js($library['library path'] . '/script/jquery.jscrollpane.min.js', array(
    'scope' => 'footer',
  ));
  drupal_add_css($library['library path'] . '/style/jquery.jscrollpane.css');

  $library = libraries_load('jquery.mousewheel');
  drupal_add_js($library['library path'] . '/jquery.mousewheel.js', array(
    'scope' => 'footer',
  ));

  libraries_load('jquery.history');

  drupal_add_js($path . '/theme/totem_common.js', array(
    'scope' => 'footer',
  ));

  // Pass any variables that JS needs to access.
  $args = drupal_get_query_parameters();
  drupal_add_js(array(
    'path_to_theme' => path_to_theme(),
    'variables' => array(
      'totem_common' => array(
        'autofade_messages' => (boolean) variable_get('totem_common_autofade_messages'),
        'autoload_modal' => (!empty($args['modal']) ? '/' . trim($args['modal'], '') : FALSE),
        'request_path' => request_path(),
      ),
    )
  ), 'setting');


  // Make these vars available to page.tpl.php.
  if (function_exists('alpha_get_theme')) {
    $theme = alpha_get_theme();

    $vars['messages'] = $theme->page['messages'];
  }

  // Force CTools modal JS to be rendered.
  // We need this so links with class="ctools-use-modal" scattered
  // throughout content, menus, etc. are properly processed by CTools.
  _totem_common_modal_link(array(
    'text' => '',
    'path' => '',
  ));

  // If user not logged in and 403, still do some full node_view stuff.
  if (!user_is_logged_in()) {
    $http_status = drupal_get_http_header('Status');
    if (stristr($http_status, '403')) {

      // Load requested node from raw path and force a node_view call;
      // this will add the required OpenGraph meta tags to head.
      // @see totem_common_node_view_alter()
      $node = _totem_common_get_node_from_request_path();
      if (!empty($node)) {
        $node = node_view($node, 'full');

        // If Facebook or proxy sharing UA, set HTTP 200 status so scrapers
        // don't choke. e.g.
        // - facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php).
        // - ShareThisFetcher/0.1.2.
        $user_agents = array('facebookexternalhit', 'sharethisfetcher');
        foreach ($user_agents as $ua) {
          // if (stristr($_SERVER['HTTP_USER_AGENT'], $ua) !== FALSE) {
          drupal_add_http_header('Status', FALSE);
          drupal_add_http_header('Status', '200 OK', FALSE);
          // }
        }
      }

      return;
    }
  }


  // Create "More" secondary tabs.
  if (!empty($vars['tabs']['#primary'])) {
    $vars['classes_array'][] = 'tabs';
    $more['secondary'] = array();
    foreach ($vars['tabs']['#primary'] as $ix => &$meta) {
      if ($ix > 5) {
        $more['secondary'][] = $meta;
        unset($vars['tabs']['#primary'][$ix]);
      }
    }

    if (!empty($more['secondary'])) {
      $more['secondary'] = theme_menu_local_tasks($more);
      $vars['tabs']['#primary'][] = array('#markup' => '<li class="last more"><a href="#" class="toggle">More</a>' . $more['secondary'] . '</li>');
    }

    // Always always remove secondary tabs.
    unset($vars['tabs']['#secondary']);
  }
  else {
    $vars['classes_array'][] = 'no-tabs';
  }


  if (!empty($vars['node'])) {

    // Denote full-page node edit forms.
    $vars['page_node_edit'] = in_array('page__node__edit', $vars['theme_hook_suggestions'])
      || in_array('page__node__edit_media_collection', $vars['theme_hook_suggestions']);

    // Community node page alters.
    if ($vars['node']->type == 'community') {

      if ($vars['page_node_edit']) {
        // Generate a simpler title than node_page_edit() does, and which
        // matches the modal edit forms.
        // @see totem_common_form_node()
        $edit_type = $vars['page']['content']['content']['content']['system_main']['#node']->type;
        $types = _totem_common_node_types();
        $vars['title'] = t("Edit @type", array('@type' => t($types[$edit_type]->name)));
        drupal_set_title($vars['title']);
      }
      else {
        drupal_set_title($vars['node']->title);
      }

      // Add "All/Private" filter links.
      $vars['tab_utility']['filter_node_status'] = _totem_common_embed_block('totem_common', 'filter_node_status');

      // Remove "Edit" and "Devel" tabs on community nodes (because they
      // ugly the tab bar up). Rely on "Manage" modal link in
      // $vars['action_links'] for edit instead.
      if (!empty($vars['tabs']['#primary'])) {
        foreach ($vars['tabs']['#primary'] as $ix => &$meta) {
          if (!empty($meta['#link'])) {
            switch ($meta['#link']['path']) {
              case 'node/%/edit':
              case 'node/%/devel':
                unset($vars['tabs']['#primary'][$ix]);
                break;
            }
          }
        }
      }


      $local_task = arg(2);
      // TODO: Exclude node-edit form pages from adjustments to community tabs?
      $vars['is_community_tab'] = !empty($local_task); //&& !$vars['page_node_edit'];
      if ($vars['is_community_tab']) {
        // Tab-page callback; use ninjutsu fighting style to rearrange renderable
        // arrays. (Note hardcoded duplication of standard Omega markup
        // in page--community.tpl.php.)
        $page = &$vars['page'];

        // Isolate data from totem_common_node_community callback.
        $type = &$page['content']['content']['content']['system_main']['#type'];
        $vars['title'] = &$page['content']['content']['content']['system_main']['#title'];
        if (!empty($vars['title'])) {
          // Prepend tab title to document title.
          $title = strip_tags($vars['title']) . ' | ' . drupal_get_title();
          drupal_set_title(check_plain($title));
        }
        $content = &$page['content']['content']['content']['system_main']['#content'];

        $vars['wrapper_css_content'] = ' ' . $page['content']['content']['content']['#data']['wrapper_css'];
        $vars['wrapper_css_sidebar_first'] = NULL;

        // Extract "action" blocks.
        $vars['blocks_action'] = array();
        $blocks_all = array();
        if (!empty($page['content']['content']['sidebar_first'])) {
          $vars['wrapper_css_sidebar_first'] = ' ' . $page['content']['content']['sidebar_first']['#data']['wrapper_css'];

          foreach ($page['content']['content']['sidebar_first'] as $key => &$element) {
            if (is_array($element)) {
              if (array_key_exists('#classes_array', $element) && in_array('action', $element['#classes_array'])) {
                $vars['blocks_action'][$key] = $element;
                unset($page['content']['content']['sidebar_first'][$key]);
              }
              elseif (array_key_exists('#block', $element)) {
                // Populate helper var for per-tab alters below.
                $blocks_all[$key] = $element;
              }
            }
          }
        }

        // Now use mystic kung-fu techniques to alter layout for specific
        // tab-page callbacks.
        $types_fullwidth = array('user');

        if ($type == 'user') {
          if (_totem_common_is_path_match('node/*/members')) {
            hide($page['content']['content']['content']['system_main']);
          }
        }

        if (variable_get('totem_media_gallery_mode')) {
          $media_types = array('media', 'media_collection');
          $types_fullwidth = array_merge($types_fullwidth, $media_types);

          // Hide main content area on Media landing page; rely on sidebar_first
          // block output instead.
          if ($type == 'media') {
            // Include path that auto-opens a media node in modal.
            $media_paths = implode("\n", array('node/*/media', 'node/*/media/view/*'));
            if (_totem_common_is_path_match($media_paths)) {
              hide($page['content']['content']['content']['system_main']);
            }
          }
        }

        if (in_array($type, $types_fullwidth)) {
          if (!empty($page['content']['content']['sidebar_first']['#grid']['columns'])) {
            $page['content']['content']['content']['#grid']['columns'] = ($page['content']['content']['content']['#grid']['columns'] + $page['content']['content']['sidebar_first']['#grid']['columns']);
          }
          $page['content']['content']['content']['#data']['wrapper_css'] = 'alpha omega';
          $page['content']['content']['content'] = array_merge($page['content']['content']['content'], $blocks_all);
          $page['content']['content']['sidebar_first'] = NULL;
        }
      }
    }

  }

}
/**
 * Implements MODULE_preprocess_region().
 */
function totem_common_preprocess_region(&$vars) {

  if (!function_exists('alpha_get_theme')) {
    return;
  }

  $theme = @alpha_get_theme();
  if (!empty($theme)) {
    $region = &$vars['elements']['#region'];

    // Expose all of $page to each region.
    // Reference $page so it can be altered globally.
    $vars['page'] = &$theme->page;

    // Set region-specific shortcut vars.
    switch ($region) {
      case 'header_first':
        $vars['site_name'] = $theme->page['site_name'];
        $vars['linked_site_name'] = l($vars['site_name'], '<front>', array('attributes' => array('rel' => 'home', 'title' => check_plain($vars['site_name']), 'class' => array('collapsed')), 'html' => TRUE));
        $vars['site_slogan'] = $theme->page['site_slogan'];
        $vars['site_name_hidden'] = $theme->page['site_name_hidden'];
        $vars['site_slogan_hidden'] = $theme->page['site_slogan_hidden'];
        $vars['logo'] = $theme->page['logo'];
        $vars['logo_img'] = $vars['logo'] ? '<img src="' . $vars['logo'] . '" alt="' . check_plain($vars['site_name']) . '" id="logo" />' : '';
        $vars['linked_logo_img'] = $vars['logo'] ? l($vars['logo_img'], '<front>', array('attributes' => array('rel' => 'home', 'title' => check_plain($vars['site_name'])), 'html' => TRUE)) : '';

        if (user_is_logged_in()) {
          $vars['linked_site_name'] = l($vars['site_name'], 'user', array('attributes' => array('rel' => 'home', 'title' => check_plain($vars['site_name']), 'class' => array('collapsed')), 'html' => TRUE));
          $vars['linked_logo_img'] = $vars['logo'] ? l($vars['logo_img'], 'user', array('attributes' => array('rel' => 'home', 'title' => check_plain($vars['site_name'])), 'html' => TRUE)) : '';
        }
        break;

      case 'header_menu':
        $vars['main_menu'] = $theme->page['main_menu'];
        $vars['secondary_menu'] = $theme->page['secondary_menu'];
        break;

      case 'content_top':
        $vars['title_prefix'] = $theme->page['title_prefix'];
        $vars['title'] = $theme->page['title'];
        $vars['title_suffix'] = $theme->page['title_suffix'];
        $vars['title_hidden'] = $theme->page['title_hidden'];
        // 2012-08-20, natemow - Dean wanted $messages moved to page.tpl.php,
        // outside of all regions. See totem_common_preprocess_page for new
        // $vars['messages'] var def.
        // $vars['messages'] = $theme->page['messages'];
        $vars['breadcrumb'] = $theme->page['breadcrumb'];
        $vars['tabs'] = $theme->page['tabs'];

        $node = isset($theme->page['node']) ? $theme->page['node'] : NULL;

        // Create rss feed.
        // First, we need to decipher current path.
        $accepted_paths = array(
          'node/*/topics',
          'node/*/media',
          'node/*/events',
          'resources/*',
        );
        // We need this case to include community overview pages for rss.
        if (!empty($node)) {
          if ($node->type == 'community') {
            $accepted_paths[] = 'node/*';
          }
        }
        $rss_approved = FALSE;
        foreach ($accepted_paths as $item) {
          if (_totem_common_is_path_match($item)) {
            $rss_approved = TRUE;
          }
        }
        if ($rss_approved) {
          $rss_url = totem_common_rss_generator(arg());
          if ($rss_url) {
            $attribs = array('class' => array('rss link'));
            $vars['rss_link'] = l(t('RSS'), $rss_url, array('attributes' => $attribs));
          }
        }

        if (!empty($node)) {

          if ($node->type == 'community') {
            $vars['title'] = $node->title;

            // Set ShareThis markup for community nodes.
            $url_options = array('entity_type' => 'node', 'entity' => $node);
            $vars['sharethis'] = _totem_common_generate_sharethis('node/' . $node->nid, $url_options);
          }

          // For resources, override the node title in header area.
          $resource_types = _totem_common_node_types(TRUE);
          if (in_array($node->type, array_keys($resource_types))) {
            $vars['title'] = $resource_types[$node->type]->name_plural;
          }
        }


        break;

      case 'content':
        $vars['action_links'] = $theme->page['action_links'];
        $vars['feed_icons'] = $theme->page['feed_icons'];
        break;
    }
  }

}
/**
 * Implements MODULE_preprocess_node().
 */
function totem_common_preprocess_node(&$vars) {

  // Add "private" indicator for unpublished nodes.
  if (empty($vars['node']->status)) {
    $vars['title_prefix']['private_indicator'] = array(
      '#markup' => '<span class="private">(private)</span>',
    );
  }

  $node_url_attributes = array(
    'title' => $vars['title'],
  );

  // Apply modal attributes as needed.
  _totem_common_modal_link_attributes_ensure($vars['node_url'], $node_url_attributes);


  // This class is added to support Drupal.ajax requests set up in
  // @see _totem_common_url_outbound_alter()
  if (stristr($vars['node_url'], '/nojs/')) {
    $node_url_attributes['class'][] = 'use-ajax';
    $node_url_attributes['rel'] = str_replace('/nojs/', '/', $vars['node_url']);
  }

  $vars['node_url_attributes'] = $node_url_attributes;


  // Add view_mode class.
  $vars['classes_array'][] = 'node-' . drupal_html_class($vars['view_mode']);

  // Add class to support custom "View more" paging.
  $vars['classes_array'][] = 'pager-entity';

  // Add class if current request = node_url.
  // TODO: Improve this goofy static trick...it's needed for the case where
  // default node is derived from master list (totem_common_node_community),
  // but not actually present in current URL path.
  static $node_url_full;
  if ('/' . request_path() == $vars['node_url'] || $node_url_full == $vars['node_url']) {
    $vars['classes_array'][] = 'active';
  }
  elseif ($vars['view_mode'] == 'full') {
    $node_url_full = $vars['node_url'];
  }


  // Everything after is only applicable to full node view.
  if ($vars['view_mode'] !== 'full') {
    return;
  }


  // Move contextual links from $title_suffix var to the end of $content.
  if (!empty($vars['title_prefix']['contextual_links'])) {
    $vars['content']['contextual_links'] = $vars['title_prefix']['contextual_links'];
    $vars['content']['contextual_links']['#weight'] = 9999;

    // Deliberately break all the contextual mod's show/hide stuff.
    $vars['content']['contextual_links']['#prefix'] = '<div class="clearfix"></div><div class="node-links-contextual clearfix">';
    $vars['content']['contextual_links']['#suffix'] = '</div>';
    $vars['content']['contextual_links']['#attributes'] = array('class' => array('links', 'inline'));

    unset($vars['title_prefix']['contextual_links']);
  }

  $vars['title_suffix'] = array(
    '#markup' => '<div class="clearfix"></div>',
  );


  // We want to change all the links of field_tags to replicate the tags block.
  // But since the filter search finds any taxonomy fields...
  // Let's also change any taxonomy field to display like the tags links
  $taxs = array();

  // First find all the taxonomy fields on the page.
  foreach (element_children($vars['elements']) as $key) {
    if (!empty($vars['elements'][$key]['#field_type'])) {
      if ($vars['elements'][$key]['#field_type'] == 'taxonomy_term_reference') {
        $taxs[] = $key;
      }
    }
  }
  // Make sure we have a taxonomy field to display.
  if (!empty($taxs)) {
    // Find the current type so we can grab the plural_path.
    $types = _totem_common_types_info();
    $ourtype = NULL;
    foreach ($types as $type) {
      if ($type->type == $vars['type']) {
        $ourtype = $type;
      }
    }

    // Change the links in the content array, ready to be rendered.
    foreach ($taxs as $field_name) {
      foreach ($vars['content'][$field_name] as $key => $item) {
        if (is_int($key)) {
          $vars['content'][$field_name][$key]['#href'] = $ourtype->path_plural;
          $vars['content'][$field_name][$key]['#options']['attributes']['class'][] = 'field-tag';
          $vars['content'][$field_name][$key]['#options']['query']['tags'] = $item['#title'];
        }
      }
    }
  }


  // Generate the ShareThis markup for this node.
  // Pass node path and data explicitly so that this function runs the path
  // through url() again with $options['absolute'] set to TRUE.
  // This is critical for media nodes when gallery mode is enabled.
  // @see totem_media_url_outbound_alter()
  $vars['sharethis'] = _totem_common_generate_sharethis("node/{$vars['node']->nid}", array(
    'entity_type' => 'node',
    'entity' => $vars['node'],
    'absolute' => TRUE
  ));

}
/**
 * Implements MODULE_preprocess_block().
 */
function totem_common_preprocess_block(&$vars) {

  $block = &$vars['block'];

  switch ($block->module) {
    default:
      $classes_array[] = drupal_html_class($block->delta);

      // Strip counter from end of delta, so common classes can be used for
      // theming.
      // @see totem_common_block_info()
      $block_type = drupal_substr($block->delta, 0, strrpos($block->delta, '_'));
      $classes_array[] = drupal_html_class('block-' . $block_type);

      // Note: handling of $vars['elements']['#classes_array'] was moved so it
      // can be applied to all render arrays, not just blocks.
      // @see totem_common_preprocess()
      $vars['classes_array'] = array_merge($vars['classes_array'], $classes_array);
      break;
  }

}

/**
 * Template preprocessor for theme('totem_common_page_communities') calls.
 */
function template_preprocess_totem_common_page_communities(&$vars) {

}
