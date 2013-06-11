<?php
/**
 * @file
 * totem_common.features.menu_custom.inc
 */

/**
 * Implements hook_menu_default_menu_custom().
 */
function totem_common_menu_default_menu_custom() {
  $menus = array();

  // Exported menu: footer-menu.
  $menus['footer-menu'] = array(
    'menu_name' => 'footer-menu',
    'title' => 'Footer menu',
    'description' => 'The <em>Footer</em> menu is presented globally to all users.',
  );
  // Exported menu: main-menu.
  $menus['main-menu'] = array(
    'menu_name' => 'main-menu',
    'title' => 'Main menu',
    'description' => 'The <em>Main</em> menu is presented globally to anonymous users.',
  );
  // Exported menu: menu-community-menu.
  $menus['menu-community-menu'] = array(
    'menu_name' => 'menu-community-menu',
    'title' => 'Community menu',
    'description' => 'The <em>Community</em> menu is presented globally to authenticated users.',
  );
  // Translatables
  // Included for use with string extractors like potx.
  t('Community menu');
  t('Footer menu');
  t('Main menu');
  t('The <em>Community</em> menu is presented globally to authenticated users.');
  t('The <em>Footer</em> menu is presented globally to all users.');
  t('The <em>Main</em> menu is presented globally to anonymous users.');


  return $menus;
}
