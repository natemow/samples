<?php
/**
 * @file
 * interactive_news.features.menu_custom.inc
 */

/**
 * Implements hook_menu_default_menu_custom().
 */
function interactive_news_menu_default_menu_custom() {
  $menus = array();

  // Exported menu: user-menu.
  $menus['user-menu'] = array(
    'menu_name' => 'user-menu',
    'title' => 'User menu',
    'description' => 'The core User menu with a log-in/log-out link plus additional links together intended to comprise a secondary menu in the header.',
  );
  // Translatables
  // Included for use with string extractors like potx.
  t('The core User menu with a log-in/log-out link plus additional links together intended to comprise a secondary menu in the header.');
  t('User menu');


  return $menus;
}
