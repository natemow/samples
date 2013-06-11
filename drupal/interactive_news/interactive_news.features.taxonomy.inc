<?php
/**
 * @file
 * interactive_news.features.taxonomy.inc
 */

/**
 * Implements hook_taxonomy_default_vocabularies().
 */
function interactive_news_taxonomy_default_vocabularies() {
  return array(
    'article_categories' => array(
      'name' => 'Article Categories',
      'machine_name' => 'article_categories',
      'description' => '',
      'hierarchy' => '0',
      'module' => 'taxonomy',
      'weight' => '0',
    ),
  );
}
