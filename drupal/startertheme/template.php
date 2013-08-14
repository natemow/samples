<?php
/**
 * @file
 * template.php
 */

/**
 * Implements theme_preprocess_page().
 */
function startertheme_preprocess_page(&$vars) {

  // Set grid layouts per sidebar presence.
  $vars['page']['grid_classes_content'] = 'small-12 large-12';
  $vars['page']['grid_classes_sidebar_first'] = 'small-12 large-3';
  $vars['page']['grid_classes_sidebar_second'] = 'small-12 large-3';

  if (!empty($vars['page']['sidebar_first'])) {
    $vars['page']['grid_classes_content'] = 'small-12 large-9 push-3';
    $vars['page']['grid_classes_sidebar_first'] = 'small-12 large-3 pull-9';
  }
  if (!empty($vars['page']['sidebar_second'])) {
    $vars['page']['grid_classes_content'] = 'small-12 large-9';
  }
  if (!empty($vars['page']['sidebar_first']) && !empty($vars['page']['sidebar_second'])) {
    $vars['page']['grid_classes_content'] = 'small-12 large-6 push-3';
    $vars['page']['grid_classes_sidebar_first'] = 'small-12 large-3 pull-6';
  }

}
/**
 * Implements theme_links().
 */
function startertheme_links__system_main_menu(&$vars) {
  global $language_url;

  $links = $vars['links'];
  $attributes = $vars['attributes'];
  $heading = $vars['heading'];

  $output = '';

  if (count($links) > 0) {
    $output = '';

    if (!empty($heading)) {
      if (is_string($heading)) {
        $heading = array(
          'text' => $heading,
          'level' => 'h2',
          'class' => 'element-invisible',
        );
      }
      $output .= '<' . $heading['level'];
      if (!empty($heading['class'])) {
        $output .= drupal_attributes(array('class' => $heading['class']));
      }
      $output .= '>' . check_plain($heading['text']) . '</' . $heading['level'] . '>';
    }

    $output .= '<ul' . drupal_attributes($attributes) . '>';

    $num_links = count($links);
    $i = 1;

    foreach ($links as $key => $link) {
      $class = array($key);

      // Add first, last and active classes to the list of links to help out themers.
      /*
      if ($i == 1) {
        $class[] = 'first';
      }
      if ($i == $num_links) {
        $class[] = 'last';
      }
      if (isset($link['href']) && ($link['href'] == $_GET['q'] || ($link['href'] == '<front>' && drupal_is_front_page())) && (empty($link['language']) || $link['language']->language == $language_url->language)) {
        $class[] = 'active';
      }
      $output .= '<li' . drupal_attributes(array('class' => $class)) . '>';
      */

      $output .= '<li>';

      $link['attributes']['title'] = check_plain($link['title']);
      $output .= l($link['title'], $link['href'], $link);

      $i++;
      $output .= "</li>\n";
    }

    $output .= '</ul>';
  }

  return $output;
}
