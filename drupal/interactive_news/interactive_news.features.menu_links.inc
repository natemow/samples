<?php
/**
 * @file
 * interactive_news.features.menu_links.inc
 */

/**
 * Implements hook_menu_default_menu_links().
 */
function interactive_news_menu_default_menu_links() {
  $menu_links = array();

  // Exported menu link: user-menu:press-room
  $menu_links['user-menu:press-room'] = array(
    'menu_name' => 'user-menu',
    'link_path' => 'press-room',
    'router_path' => 'press-room',
    'link_title' => 'Press Room',
    'options' => array(
      'attributes' => array(
        'title' => '',
      ),
    ),
    'module' => 'menu',
    'hidden' => '0',
    'external' => '0',
    'has_children' => '0',
    'expanded' => '0',
    'weight' => '-50',
  );
  // Translatables
  // Included for use with string extractors like potx.
  t('Press Room');


  return $menu_links;
}
