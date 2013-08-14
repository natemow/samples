<?php

/**
 * @file
 * html.tpl.php
 */
?><!DOCTYPE html>
<!--[if lt IE 9]> <html class="lt-ie9" lang="<?php print $language->language; ?>" dir="<?php print $language->dir; ?>"> <![endif]-->
<!--[if (gt IE 9)|!(IE)]><!--> <html lang="<?php print $language->language; ?>" dir="<?php print $language->dir; ?>"> <!--<![endif]-->
<head>
  <?php print $head; ?>
  <meta name="viewport" content="width=device-width, initial-scale=1, minimum-scale=1, maximum-scale=1.6, user-scalable=yes" />
  <title><?php print $head_title; ?></title>
  <link rel="shortcut icon" type="image/x-icon" href="/<?php print path_to_theme(); ?>/favicon.ico" />
  <link rel="shortcut icon" type="image/vnd.microsoft.icon" href="/<?php print path_to_theme(); ?>/favicon.ico" />
  <link rel="icon" type="image/png" href="/<?php print path_to_theme(); ?>/favicon.png" />
  <?php print $styles; ?>
  <script type="text/javascript" src="/<?php print path_to_theme(); ?>/assets/js/custom.modernizr.js"></script>
</head>
<body>
  <div id="skip-link">
    <a href="#main-content" class="element-invisible element-focusable"><?php print t('Skip to main content'); ?></a>
  </div>
  <?php print $page_top; ?>
  <?php print $page; ?>
  <?php print $scripts; ?>
  <?php print $page_bottom; ?>
</body>
</html>
