<?php
/**
 * @file
 * interactive_news.features.inc
 */

/**
 * Implements hook_node_info().
 */
function interactive_news_node_info() {
  $items = array(
    'article' => array(
      'name' => t('Article'),
      'base' => 'node_content',
      'description' => t('Use this type to create a News Article.'),
      'has_title' => '1',
      'title_label' => t('Title'),
      'help' => '',
    ),
  );
  return $items;
}
