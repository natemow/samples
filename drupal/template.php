<?php
/**
 * @file
 * template.php
 */

/**
 * Implements hook_form_alter().
 */
function nprdd_ui_form_alter(&$form, &$form_state, $form_id) {
  if (strpos($form_id, 'webform_client_form') === 0) {
    // We can use a simple loop only b/c this array is flat...
    foreach ($form['submitted'] as &$elem) {
      if (in_array($elem['#type'], array('textfield', 'webform_email', 'textarea', 'select')) && !empty($elem['#title'])) {

        if ($elem['#required']) {
          // Since label will be hidden, tack on the required marker after input.
          // Note: had to change the variable 'webform_allowed_tags' to avoid
          // the span being filtered out.
          // @see _webform_filter_xss(), webform_variable_get()
          $elem['#field_suffix'] = '<span class="form-required" title="This field is required">*</span>';
        }

        if ($elem['#type'] == 'select') {
          // Make sure there's an empty value that will not pass validation if
          // field is required.
          if ($elem['#required']) {
            $elem['#empty_value'] = '';
          }
          // Use title as empty option (matching with pretty placeholders).
          if (!empty($elem['#title'])) {
            $elem['#empty_option'] = "- " . strip_tags($elem['#title']) . " -";
          }
        }
        else {
          // Use pretty placeholder.
          $elem['#attributes']['placeholder'] = strip_tags($elem['#title']);
        }
      }
    }

    // Pretty button.
    $form['actions']['submit']['#attributes']['class'][] = 'button';
    $form['actions']['submit']['#attributes']['class'][] = 'round';
  }
}
/**
 * Implements hook_form_FORM_ID_alter().
 */
function nprdd_ui_form_search_form_alter(&$form, &$form_state, $form_id) {

  $form['basic']['#attributes']['class'][] = 'search-keywords';
  $form['basic']['submit']['#prefix'] = '<div class="form-item form-actions"><div class="form-submit-wrapper">';
  $form['basic']['submit']['#suffix'] = '</div></div>';
  $form['basic']['submit']['#attributes']['class'][] = 'button';

}
/**
 * Implements hook_form_FORM_ID_alter().
 */
function nprdd_ui_form_search_block_form_alter(&$form, &$form_state, $form_id) {

  $form['#attributes']['class'][] = 'search-keywords';

  $form['search_block_form']['#attributes']['title']
    = $form['search_block_form']['#attributes']['placeholder'] = 'Keywords...';

  $form['actions']['#prefix'] = '<div class="form-item form-actions">';
  $form['actions']['#suffix'] = '</div>';

  $form['actions']['submit']['#prefix'] = '<div class="form-submit-wrapper">';
  $form['actions']['submit']['#suffix'] = '</div>';
  $form['actions']['submit']['#attributes']['class'][] = 'button';

}
/**
 * Implements theme_button().
 */
function nprdd_ui_button($vars) {
  $element = $vars['element'];
  $element['#attributes']['type'] = 'submit';
  element_set_attributes($element, array('id', 'name', 'value'));

  $element['#attributes']['class'][] = 'form-' . $element['#button_type'];
  if (!empty($element['#attributes']['disabled'])) {
    $element['#attributes']['class'][] = 'form-button-disabled';
  }

  // Add class to pull in Foundation stuff.
  $element['#attributes']['class'][] = 'button';

  return '<input' . drupal_attributes($element['#attributes']) . ' />';
}


/**
 * Implements template_preprocess_html().
 */
function nprdd_ui_preprocess_html(&$vars) {

  // Sanitize title (previously overridden in preprocess_page).
  // @see nprdd_ui_preprocess_page()
  $title = str_replace(array('&lt;', '&gt;'), array('<','>'), html_entity_decode($vars['head_title'], ENT_NOQUOTES));
  $vars['head_title'] = filter_xss($title, array());

  $vars['classes_array'][] = drupal_html_class(variable_get('theme_default'));

  // HTML5 charset declaration.
  $vars['system_meta_content_type']['#attributes'] = array(
    'charset' => 'utf-8',
  );

  $meta = array();

  // Optimize mobile viewport.
  $meta['viewport'] = array(
    '#type' => 'html_tag',
    '#tag' => 'meta',
    '#attributes' => array(
      'name' => 'viewport',
      'content' => 'width=device-width, initial-scale=1, maximum-scale=1, minimum-scale=1, user-scalable=no',
    ),
  );

  // Force IE to use Chrome Frame if installed.
  $meta['ie_chrome_frame'] = array(
    '#type' => 'html_tag',
    '#tag' => 'meta',
    '#attributes' => array(
      'content' => 'ie=edge, chrome=1',
      'http-equiv' => 'x-ua-compatible',
    ),
  );

  // Remove image toolbar in IE.
  $meta['ie_image_toolbar'] = array(
    '#type' => 'html_tag',
    '#tag' => 'meta',
    '#attributes' => array(
      'http-equiv' => 'ImageToolbar',
      'content' => 'false',
    ),
  );

  // Add meta elements to head.
  foreach ($meta as $key => $val) {
    drupal_add_html_head($val, $key);
  }


  if (!empty($vars['page']['content']['views_menu_nodes-jump_targets'])) {
    $vars['classes_array'][] = 'has-main-menu-children';
  }
  if (!empty($vars['page']['highlighted'])) {
    $vars['classes_array'][] = 'footer-highlighted';
  }

}
/**
 * Goofball helper function to do special UI stuff per menu item child nodes.
 */
function _nprdd_ui_preprocess_page_per_menu_parent(&$vars) {

  // Use it like this in your caller preprocess_page:
  //   $vars['menu_node_links_parent_mlids'] = array(311, 312);
  //   _nprdd_ui_preprocess_page_per_menu_parent($vars);
  if (!empty($vars['menu_node_links_parent_mlids']) && !empty($vars['node'])) {
    $node = &$vars['node'];
    foreach ($node->menu_node_links as $link) {
      foreach ($vars['menu_node_links_parent_mlids'] as $mlid) {
        if ($link->p1 == $mlid) {

          $section_parent = menu_node_get_node($link->p1);

          // Prefix title with "section" title.
          if ($link->p1 !== $link->mlid) {
            $vars['title'] = "{$section_parent->title}: <span>{$vars['title']}</span>";
          }
          // Add bg image class only for 1st and 2nd level items.
          if ($link->depth <= 2) {
            $vars['classes_array'][] = "bg-image-node-{$section_parent->nid}";
          }
        }
      }
    }
  }

}
/**
 * Implements template_preprocess_page().
 */
function nprdd_ui_preprocess_page(&$vars) {

  $vars['logo_img'] = '';
  if (!empty($vars['logo'])) {
    $vars['logo_img'] = theme('image', array(
      'path'  => $vars['logo'],
      'alt'   => strip_tags($vars['site_name']) . ' ' . t('logo'),
      'title' => strip_tags($vars['site_name']) . ' ' . t('Home'),
            'attributes' => array(
        'class' => array('logo'),
      ),
    ));
  }

  $vars['linked_logo']  = '';
  if (!empty($vars['logo_img'])) {
    $vars['linked_logo'] = l($vars['logo_img'], '<front>', array(
      'attributes' => array(
        'rel'   => 'home',
        'title' => strip_tags($vars['site_name']) . ' ' . t('Home'),
      ),
      'html' => TRUE,
    ));
  }

  $vars['linked_site_name'] = '';
  if (!empty($vars['site_name'])) {
    $vars['linked_site_name'] = l($vars['site_name'], '<front>', array(
      'attributes' => array(
        'rel'   => 'home',
        'title' => strip_tags($vars['site_name']) . ' ' . t('Home'),
      ),
    ));
  }

  // Allow span tags in title; reset for head title.
  // @see nprdd_ui_preprocess_html()
  if ($title = drupal_get_title()) {
    $title = html_entity_decode($title);
    $vars['title'] = filter_xss($title, array('span'));
  }

  ////////////////////////////////////////////////////////////////////////////
  // If current path is in top-level main menu hrefs, or is the hp node, it's
  // eligible to have .bg-image classes assigned.
  /*
  $menu_main = menu_navigation_links('main-menu', 0);
  $menu_main_home = variable_get('site_frontpage');
  $menu_main_href = array();
  foreach ($menu_main as $item) {
    $menu_main_href[] = $item['href'];
  }

  if (!empty($menu_main_home)) {
    $menu_main_home = menu_get_item($menu_main_home);
    if (!in_array($menu_main_home['href'], $menu_main_href)) {
      array_unshift($menu_main_href, $menu_main_home['href']);
    }
  }
  */

  $current_path = drupal_get_destination();
  $current_path = $current_path['destination'];
  // 2013-06-05, natemow: Per PRSS ContentDepot exception, we'll need ability
  // to apply CSS bg image for pretty much any node.
  //if (in_array($current_path, $menu_main_href)) {
    $vars['classes_array'][] = 'bg-image';
    $vars['classes_array'][] = drupal_html_class("bg-image-{$current_path}");
  //}
  ////////////////////////////////////////////////////////////////////////////

  // Dynamic sidebars (reset of base theme).
  if (!empty($vars['page']['sidebar_first']) && !empty($vars['page']['sidebar_second'])) {
    $vars['main_grid'] = 'large-6 push-4';
    $vars['sidebar_first_grid'] = 'large-4 pull-6';
    $vars['sidebar_sec_grid'] = 'large-2';
  }
  elseif (empty($vars['page']['sidebar_first']) && !empty($vars['page']['sidebar_second'])) {
    $vars['main_grid'] = 'large-8';
    $vars['sidebar_first_grid'] = '';
    $vars['sidebar_sec_grid'] = 'large-4';
  }
  elseif (!empty($vars['page']['sidebar_first']) && empty($vars['page']['sidebar_second'])) {
    $vars['main_grid'] = 'large-8 push-4';
    $vars['sidebar_first_grid'] = 'large-4 pull-8';
    $vars['sidebar_sec_grid'] = '';
  }
  else {
    $vars['main_grid'] = 'large-12';
    $vars['sidebar_first_grid'] = '';
    $vars['sidebar_sec_grid'] = '';
  }



  // Hide and pre-render these blocks.
  hide($vars['page']['content']['system_main-menu']);
  hide($vars['page']['content']['system_main']);

  // Fugly stuff to catch first homepage menu block.
  $intro_block_front = NULL;
  if ($vars['is_front']) {
    $found_system_main = FALSE;
    foreach ($vars['page']['content'] as $key => &$meta) {
      if (!empty($meta['#block']) && is_object($meta['#block'])) {
        if ($found_system_main) {

          if ($meta['#block']->module == 'menu_block') {
            $vars['page']['content'][$key]['#prefix'] = '<div class="row"><div class="columns large-12">';
            $vars['page']['content'][$key]['#suffix'] = '</div></div>';
          }

          $intro_block_front = $key;
          hide($vars['page']['content'][$key]);
        }

        $found_system_main = ($meta['#block']->module == 'system' && $meta['#block']->delta == 'main');
      }
    }
  }

  // Pre-render to local vars so stuff is isolated from
  // "jump target" #prefix/#suffix logic below.
  $vars['main_menu'] = render($vars['page']['content']['system_main-menu']);
  $vars['main_content'] = render($vars['page']['content']['system_main']);
  $vars['header'] = (!empty($vars['page']['header']) ? render($vars['page']['header']) : NULL);
  $vars['intro_block_front'] = (!empty($intro_block_front) ? render($vars['page']['content'][$intro_block_front]) : NULL);

  if (!array_key_exists('views_menu_nodes-jump_targets', $vars['page']['content'])) {
    if ($content = render($vars['page']['content'])) {
      $vars['page']['content']['#prefix'] = '<div class="row">';
      $vars['page']['content']['#suffix'] = '</div>';
    }
  }
  else {
    foreach ($vars['page']['content'] as $key => &$element) {
      if (empty($element['#printed']) && drupal_substr($key, 0, 1) !== '#') {
        $row_wrap = TRUE;
        if (in_array($key, array('views_menu_nodes-jump_targets'))) {
          $row_wrap = FALSE;
        }

        $element['#prefix'] = '<div class="' . drupal_html_class($key) . '">' . ($row_wrap ? '<div class="row">' : '');
        $element['#suffix'] = '</div>' . ($row_wrap ? '</div>' : '');
      }
    }
  }

}
/**
 * Implements template_preprocess_node().
 */
function nprdd_ui_preprocess_node(&$vars) {

  $vars['title_attributes_array']['class'][] = 'node-title';

  // Add "Back to top" link.
  $vars['title_prefix']['subnav-return']['#markup'] = l('<span>Back to top</span>', current_path(), array(
    'fragment' => 'subnav',
    'html' => TRUE,
    'attributes' => array(
      'class' => array('menu-jump-top'),
    ),
  ));

  // Add corresponding menu item's icon image to node's title_prefix.
  if (module_exists('menu_node')) {
    foreach ($vars['node']->menu_node_links as $link) {
      if ($link->menu_name == 'main-menu') {

        $options = unserialize($link->options);
        $icon = (!empty($options['icon']) ? $options['icon'] : NULL);

        if (!empty($icon)) {
          $vars['title_prefix']['menu_image']['#markup'] = '<span class="menu-image">' . theme_image(array(
            'path' => path_to_theme() . '/images/blank.png',
            'attributes' => array(
              'class' => array('icon', ($icon == 'default' ? 'space-segment' : $icon),),
            ),
          )) . '</span>';
        }
      }
    }
  }

}
/**
 * Implements template_preprocess_block().
 */
function nprdd_ui_preprocess_block(&$vars) {

  // Convenience variable for block headers.
  $title_class = &$vars['title_attributes_array']['class'];

  // Generic block header class.
  $title_class[] = 'block-title';

  // In the header region visually hide block titles.
  if ($vars['block']->region == 'header') {
    $title_class[] = 'element-invisible';
  }

  // Add a unique class for each block for styling.
  $vars['classes_array'][] = $vars['block_html_id'];

  $vars['content_attributes_array']['class'][] = 'block-content';

  // We need to set the two tab variables for the outage block.
  if ($vars['block']->module == 'nprdd_common' && $vars['block']->delta == 'outage') {
    $vars['outage'] = $vars['elements']['#outage'];
    $vars['alignment'] = $vars['elements']['#alignment'];
  }
}
/**
 * Implements template_views_view().
 */
function nprdd_ui_preprocess_views_view(&$vars) {
  // We need to populate a variable of all results for the custom tpl.
  if ($vars['display_id'] == 'alignment_embed') {
    if (!empty($vars['view']->result)) {
      foreach ($vars['view']->result as $node) {
        $vars['results'][$node->nid] = node_load($node->nid);
      }
    }
  }
}
/**
 * Implements THEME_preprocess_form().
 */
function nprdd_ui_preprocess_form(&$vars) {
  // Add 'custom' class to invoke Foundation's custom form element
  // replacements (only for <select>s in our case).
  // @see foundation.forms.js
  $vars['element']['#attributes']['class'][] = 'custom';
}
/**
 * Implements theme_menu_tree().
 */
function nprdd_ui_menu_tree($vars) {

  // Add class to menu ul per immediate child li count.
  // Load the current HTML into a DOMDocument to count the links.
  libxml_use_internal_errors(TRUE);
  $doc = new DOMDocument;
  $doc->strictErrorChecking = false;
  $doc->loadHTML('<?xml encoding="UTF-8"><ul id="non-existent-id">' . $vars['tree'] . '</ul>');

  // TODO: Strip UNICODE chars!!!
  // ...or just don't copy/paste text from Photoshop...
//  foreach (libxml_get_errors() as $error) {
//    dpm($error);
//  }
//  libxml_clear_errors();

  // XPath query to find only top-level <li>s.
  $xpath = new DOMXPath($doc);
  $items = $xpath->query('//ul[@id="non-existent-id"]/li');

  return '<ul class="menu count-' . $items->length . '">' . $vars['tree'] . '</ul>';
}
/**
 * Helper function to check use_parent_with_fragment link option.
 * @see nprdd_common_form_menu_edit_item_alter()
 */
function _nprdd_ui_menu_link__main_menu_link_use_parent_with_fragment(&$item) {
  // Goofy check here...menus were already built for 3 sites by the
  // time the use_parent_with_fragment option was added to the menu
  // link form. Needed new option to account for PRSS exception in 1
  // particular menu branch.
  $use_parent_with_fragment = TRUE;

  if (isset($item['#localized_options']['use_parent_with_fragment'])) {
    if (empty($item['#localized_options']['use_parent_with_fragment'])) {
      $use_parent_with_fragment = FALSE;
      $item['#localized_options']['fragment_parent_set'] = TRUE;
    }
  }

  return $use_parent_with_fragment;
}
/**
 * Implements theme_menu_link().
 */
function nprdd_ui_menu_link__main_menu($vars) {
  $element = $vars['element'];
  $parent = $vars['element'];
  $sub_menu = '';
  $depth = $element['#original_link']['depth'];

  $element['#localized_options']['html'] = TRUE;
  $element['#attributes']['class'][] = 'level-' . $depth;

  ////////////////////////////////////////////////////////////////////////
  // Child item icon image + description magic.
  $title = array();
  $icon = (!empty($element['#localized_options']['icon']) ? $element['#localized_options']['icon'] : '');
  $description = (!empty($element['#localized_options']['attributes']['title']) ? $element['#localized_options']['attributes']['title'] : '');

  // Prepend parent's description value as sub-menu item.
  if (!empty($element['#below']) && !empty($description)) {
    $parent['#original_link']['depth'] = ($depth + 1);

    // Prevent recursion.
    $parent['#below'] = array();
    $parent['#attributes']['class'][] = 'level-parent';

    array_unshift($element['#below'], $parent);
  }

  // Hide description for top-level items, unless it's a menu_block.
  if ($depth == 1) {
    if (empty($element['#bid'])) {
      $description = FALSE;

      // Add a class to top-level items for extra, special styling per...
      $element['#attributes']['class'][] = 'menu-mlid-' . $element['#original_link']['mlid'];
    }
  }

  // Add hover indicator.
  $element['#title'] = '<span class="arrow"></span>' . $element['#title'];

  // Set extra markup for border (gross but required for greatest link click
  // area and meeting design requirements).
  $title[] = '<span class="border"></span>';
  // Set icon image markup.
  if (!empty($icon)) {
    $title[] = '<span class="image">' . theme_image(array(
      'path' => path_to_theme() . '/images/blank.png',
      'attributes' => array(
        'class' => array('icon', ($icon == 'default' ? 'space-segment' : $icon),),
      ),
    )) . '</span>';
  }
  // Set title markup.
  $title[] = '<span class="title' . (empty($icon) ? ' fullwidth' : '') . '">' . $element['#title'] . '</span>';
  // Set description markup.
  if (!empty($description)) {
    $title[] = '<span class="description">' . $description . '</span>';
  }

  // Clean up title; content is now part of the link text.
  unset($element['#localized_options']['attributes']['title']);

  $element['#title'] = implode("\n", $title);
  ////////////////////////////////////////////////////////////////////////


  ////////////////////////////////////////////////////////////////////////
  // Menu fragment magic.
  if (!empty($element['#below'])) {
    $child_counter = 0;
    foreach ($element['#below'] as &$item) {
      if (!empty($item['#href'])) {
        $child_counter++;
        $item['#attributes']['class'][] = 'item-' . $child_counter;

        // Check that this item should be handled with /parent-path#item-path.
        $use_parent_with_fragment = _nprdd_ui_menu_link__main_menu_link_use_parent_with_fragment($item);
        if ($use_parent_with_fragment) {
          $is_parent = ($item['#original_link']['mlid'] == $parent['#original_link']['mlid']);
          if (!$is_parent) {
            $item['#localized_options']['fragment'] = drupal_html_class($item['#href']);
            $item['#localized_options']['fragment_parent_set'] = TRUE;
          }

          $item['#href'] = $parent['#href'];
        }
      }
    }

    $sub_menu = drupal_render($element['#below']);
  }
  else {
    // Catch menu_block items.
    if (empty($element['#localized_options']['fragment_parent_set'])) {
      if (!empty($element['#original_link']['plid'])) {

        // Check that this item should be handled with /parent-path#item-path.
        $use_parent_with_fragment = _nprdd_ui_menu_link__main_menu_link_use_parent_with_fragment($element);
        if ($use_parent_with_fragment) {
          $parent = menu_node_get_parent($element['#original_link'], NULL);
          $element['#localized_options']['fragment'] = drupal_html_class($element['#href']);
          $element['#localized_options']['fragment_parent_set'] = TRUE;
          $element['#href'] = $parent['href'];
        }

      }
    }
  }
  ////////////////////////////////////////////////////////////////////////

  // Output link and list item.
  $output = l($element['#title'], $element['#href'], $element['#localized_options']);

  return '<li' . drupal_attributes($element['#attributes']) . '>' . $output . $sub_menu . "</li>\n";
}
/**
 * Implements theme_breadrumb().
 */
function nprdd_ui_breadcrumb($vars) {

  $breadcrumb = $vars['breadcrumb'];

  if (!empty($breadcrumb)) {
    $separator = '<span class="arrow"></span>';

    $breadcrumbs = '<h2 class="element-invisible">' . t('You are here') . '</h2>';
    $breadcrumbs .= '<ul class="menu breadcrumbs">';

    foreach ($breadcrumb as $key => $value) {
      $value = str_ireplace('</a>', $separator . '</a>', $value);
      $breadcrumbs .= '<li>' . $value . '</li>';
    }

    $title = strip_tags(drupal_get_title());
    $breadcrumbs .= '<li class="current"><a href="#">' . $title. '</a></li>';
    $breadcrumbs .= '</ul>';

    return $breadcrumbs;
  }
}

function nprdd_ui_js_alter(&$js) {
  // Only include zepto.js if browser supports it. Technically Foundation only
  // needs Zepto *or* jQuery, but we need to keep jQuery for Drupal and our JS.
  // So at least avoid including Zepto when it breaks stuff (mainly, IE).
  // @see http://foundation.zurb.com/docs/javascript.html
  $zepto_js_path = path_to_theme() . '/js/zepto.js';
  $js[$zepto_js_path]['type'] = 'inline';
  $js[$zepto_js_path]['data'] = "if ('__proto__' in {}) { document.write('<script type=\"text/javascript\" src=\"/" . $zepto_js_path . "\"></script>'); }";
}