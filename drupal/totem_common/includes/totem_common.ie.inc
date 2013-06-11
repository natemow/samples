<?php
/**
 * @file
 * totem_common.ie.inc
 */

/**
 * Helper function to check UA.
 */
function _totem_common_is_ie($versions = array()) {

  if (empty($versions)) {
    return strpos($_SERVER['HTTP_USER_AGENT'], 'MSIE') !== FALSE;
  }
  else {
    foreach ($versions as $v) {
      if (strpos($_SERVER['HTTP_USER_AGENT'], 'MSIE ' . (string) $v . '.0') !== FALSE) {
        return TRUE;
      }
    }
  }

  return FALSE;
}

/**
 * Implements hook_boot().
 */
function totem_common_boot() {
  // Disable anonymous-user page caching for IE 7 & 8 only.
  // This allows critical jquery.form.js version-swapperoo to happen;
  // @see totem_common_library_alter()
  //
  // CREDIT: This technique for preventing serving a page from the cache
  // is borrowed directly from the User-Agent Cache Bypass module.
  // @see http://drupal.org/project/ua_cache_bypass
  // @see ua_cache_bypass_boot()
  // Regarding understanding of serving cached pages in bootstrap:
  // @see _drupal_bootstrap_page_cache()
  global $user;

  if ($user->uid == 0 && variable_get('cache', 1) && _totem_common_is_ie(array(7, 8))) {
    header('X-Drupal-Cache: MISS');
    ob_start();

    // Continue the bootstrap process.
    drupal_bootstrap(DRUPAL_BOOTSTRAP_FULL);

    // Execute the page callback and exit.
    menu_execute_active_handler();
    exit;
  }
}
/**
 * Implements hook_exit().
 */
function totem_exit() {
  // This hook is invoked in drupal_page_footer(), and does happen before
  // the current page is stored into cache via drupal_page_set_cache().
  // We denote this page as not cacheable for IE 7 & 8, to avoid storing
  // the page with alternative version of jquery.form.js.
  if (_totem_common_is_ie(array(7, 8))) {
    drupal_page_is_cacheable(FALSE);
  }
}
/**
 * Implements hook_library_alter().
 */
function totem_common_library_alter(&$libraries, $module) {

  // IE8- hackery here...need to serve an alternate version of jquery.form.js
  // (3.09) in that case. Some profile features will fail with this
  // (e.g. plupload), but key functionality such as login will work with this
  // alter in place.
  //
  // @see http://drupal.org/node/1675794
  // @see http://drupal.org/node/1737152
  // @see clientside_validation_library_alter()
  //
  // We are updating just the system module. For all other cases we return.
  if ($module != 'system') {
    return;
  }

  $serve_jquery_form_309 = _totem_common_is_ie(array(7, 8));

  // Replace jQuery Form plugin.
  if ($serve_jquery_form_309) {
    $path = drupal_get_path('module', 'clientside_validation');
    $libraries['jquery.form']['js']['misc/jquery.form.js']['data'] = $path . '/jquery.form.js';
    $libraries['jquery.form']['version'] = '3.09';
  }
}
