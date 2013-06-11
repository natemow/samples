<?php
/**
 * @file
 * interactive_news.pages.inc
 */

/**
 * List page for article nodes.
 */
function interactive_news_page_list($arg1, $arg2) {

  $menu = menu_get_item();

  $efq_params = _interactive_news_query_params();

  if (!empty($arg1)) {
    if ($arg1 == 'category') {
      // Term filter.
      $term = str_replace('-', ' ', $arg2);
      $term = taxonomy_get_term_by_name($term);
      $term = array_pop($term);

      $efq_params['meta_data'][] = array(
        'key' => 'filter_term', 'object' => $term,
      );
      $efq_params['field_conditions'][] = array(
        'field' => 'field_article_terms', 'column' => 'tid', 'value' => $term->tid,
      );

      drupal_set_title("{$menu['title']}: {$term->name}");
    }
    elseif ($arg1 == 'archive') {

      if (drupal_strlen($arg2) == 4 && is_numeric($arg2)) {
        // Year filter.
        $date1 = date('Y-m-d H:i:s', strtotime("Jan 01 {$arg2} 12:00 AM"));
        $date2 = date('Y-m-d H:i:s', strtotime("Dec 31 {$arg2} 11:59 PM"));

        drupal_set_title("{$menu['title']}: {$arg2}");
      }
      else {
        // Year+month filter.
        $date1 = date('Y-m-d H:i:s', strtotime("{$arg2} 12:00 AM"));
        $date2 = date('Y-m-d H:i:s', strtotime("{$arg2} 12:00 AM +1 month -1 minute"));

        drupal_set_title("{$menu['title']}: " . date('F Y', strtotime($arg2)));
      }

      $efq_params['meta_data'][] = array(
        'key' => 'filter_archive_date_range', 'object' => (object) array('start' => $date1, 'end' => $date2),
      );
      $efq_params['field_conditions'][] = array(
        'field' => 'field_article_date', 'column' => 'value', 'operator' => 'BETWEEN', 'value' => array($date1, $date2),
      );
    }
  }

  // Alter params for this page callback specifically.
  $efq_param_type = __FUNCTION__;
  drupal_alter('interactive_common_EFQ_params', $efq_params, $efq_param_type);

  $build = array(
    '#theme' => 'interactive_common_paged_entities',
    '#query' => _interactive_common_efq($efq_params),
  );

  return $build;
}
