<?php
/**
 * @file
 * interactive_common.block.inc
 */

// Hook implementations.
/**
 * Implements hook_block_info().
 */
function interactive_news_block_info() {
  $blocks = array();

  $blocks['article_recent'] = array(
    'info' => t('Interactive News: Recent News'),
    'region' => 'content',
    'status' => 1,
    'cache' => DRUPAL_CACHE_GLOBAL,
    'title' => '',
    'visibility' => BLOCK_VISIBILITY_LISTED,
    'pages' => '<front>',
  );

  $pages = array('news', 'news/*', 'press-room', 'press-room/*');
  $pages = implode("\r\n", $pages);
  $blocks['article_year'] = array(
    'info' => t('Interactive News: Publish year filter'),
    'region' => 'content',
    'status' => 1,
    'cache' => DRUPAL_CACHE_GLOBAL,
    'title' => '<none>',
    'visibility' => BLOCK_VISIBILITY_LISTED,
    'pages' => $pages,
  );

  $blocks['article_terms'] = array(
    'info' => t('Interactive News: Categories'),
    'region' => 'sidebar_second',
    'status' => 1,
    'cache' => DRUPAL_CACHE_GLOBAL,
    'title' => '',
    'visibility' => BLOCK_VISIBILITY_LISTED,
    'pages' => $pages,
  );

  $blocks['article_archives'] = array(
    'info' => t('Interactive News: Archives'),
    'region' => 'sidebar_second',
    'status' => 1,
    'cache' => DRUPAL_CACHE_GLOBAL,
    'title' => '',
    'visibility' => BLOCK_VISIBILITY_LISTED,
    'pages' => $pages,
  );

  return $blocks;
}
/**
 * Implements hook_block_configure().
 */
function interactive_news_block_configure($delta = '') {
  $form = array();

  switch ($delta) {
    case 'article_recent':
      $form['block_recent_number'] = array(
        '#type' => 'select',
        '#title' => t("Number of Recent Posts"),
        '#description' => t("Select how many posts should appear in the block."),
        '#options' => array(
          '5' => '5',
          '6' => '6',
          '7' => '7',
          '8' => '8',
          '9' => '9',
          '10' => '10'
        ),
        '#default_value' => variable_get('interactive_news_article_recent_number', '5'),
        '#required' => FALSE,
      );

      break;

    case 'article_terms':
      $form['terms_all_link'] = array(
        '#type' => 'checkbox',
        '#title' => t("\"All\" link"),
        '#description' => t("Include an \"All\" link to view articles in all categories (i.e. /news page)."),
        '#default_value' => variable_get('interactive_news_terms_all_link', FALSE),
      );

      break;
  }

  return $form;
}
/**
 * Implements hook_block_save().
 */
function interactive_news_block_save($delta = '', $edit = array()) {
  switch ($delta) {
    case 'article_recent':
      variable_set('interactive_news_article_recent_number', $edit['block_recent_number']);
      break;

    case 'article_terms':
      variable_set('interactive_news_terms_all_link', $edit['terms_all_link']);
      break;
  }
}
/**
 * Implements hook_block_view().
 */
function interactive_news_block_view($delta = '', $context = '') {
  $block = NULL;

  // View blocks.
  $callback = "_interactive_news_block_view_{$delta}";
  if (function_exists($callback)) {
    $block = $callback($delta, $context);
  }

  return $block;
}

// Block view callbacks.
/**
 * Block-view callback.
 */
function _interactive_news_block_view_article_year($delta, $context) {

  $efq_params = _interactive_news_query_params();
  $efq_params = array_merge($efq_params, array(
    'page_limit' => NULL,
  ));

  // Alter params for this block callback specifically.
  $efq_param_type = __FUNCTION__;
  drupal_alter('interactive_common_EFQ_params', $efq_params, $efq_param_type);

  $block = module_invoke('interactive_common', 'block_view', 'archives_type', array(
    $efq_params,
    'Y',
    'Y',
    FALSE,
  ));

  return $block;
}
/**
 * Block-view callback.
 */
function _interactive_news_block_view_article_terms($delta, $context) {

  $bundle = 'article';
  $is_press_room = _interactive_news_query_is_press_room();

  // Get term field data.
  $term_fields = _interactive_common_field_info_taxonomy(array(
    'bundle' => $bundle,
    'include_bundle_count' => TRUE,
    'extra_count_args' => array(
      'field_conditions' => array(
        array('field' => 'field_article_press_release', 'column' => 'value', 'value' => $is_press_room),
      ),
    )
  ));

  $block = module_invoke('interactive_common', 'block_view', 'terms_type', array(
    $bundle,
    $term_fields,
  ));

  if (variable_get('interactive_news_terms_all_link', FALSE)) {
    $item = l(t("All"), 'news');
    array_unshift($block['content']['#items'], $item);
  }

  return $block;
}
/**
 * Block-view callback.
 */
function _interactive_news_block_view_article_archives($delta, $context) {

  $efq_params = _interactive_news_query_params();
  $efq_params = array_merge($efq_params, array(
    'property_order_by' => NULL,
    'page_limit' => NULL,
  ));

  // Alter params for this block callback specifically.
  $efq_param_type = __FUNCTION__;
  drupal_alter('interactive_common_EFQ_params', $efq_params, $efq_param_type);

  $block = module_invoke('interactive_common', 'block_view', 'archives_type', array(
    $efq_params,
    'F Y',
    'Y-m',
    TRUE,
  ));

  return $block;
}
/**
 * Block-view callback.
 */
function _interactive_news_block_view_article_recent($delta, $context) {

  $efq_params = _interactive_news_query_params();
  $efq_params = array_merge($efq_params, array(
    'page_limit' => variable_get('interactive_news_article_recent_number', PAGE_SIZE_LISTS_TEASER),
  ));

  // Alter params for this block callback specifically.
  $efq_param_type = __FUNCTION__;
  drupal_alter('interactive_common_EFQ_params', $efq_params, $efq_param_type);

  $query = _interactive_common_efq($efq_params);

  $block['subject'] = l(t('Recent News'), 'news');
  $block['content'] = $query->results;

  return $block;
}
