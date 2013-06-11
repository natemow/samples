<?php
/**
 * @file
 * node--resource.tpl.php
 */
?>

<div<?php print $attributes; ?>>
  <div<?php print $content_attributes; ?>>

    <?php if ($view_mode == 'teaser'): ?>

      <?php print render($title_prefix); ?>
      <h2<?php print $title_attributes; ?>><a href="<?php print $node_url; ?>" <?php print drupal_attributes($node_url_attributes); ?>><?php print $title; ?></a></h2>
      <?php print render($title_suffix); ?>

      <div class="resource-body">
      <?php print render($content['body']); ?>
      </div>

    <?php elseif ($view_mode == 'recent_entity'): ?>

      <?php print theme('totem_activity_recent_entity', $variables); ?>

    <?php else: ?>

      <?php print render($title_prefix); ?>
      <h2<?php print $title_attributes; ?>><?php print $title ?></h2>
      <?php print render($title_suffix); ?>

      <?php
      hide($content['comments']);
      hide($content['links']);
      print render($content);
      print render($sharethis);
      ?>

      <?php if ($submitted): ?>
        <div class="submitted">
          <?php print $submitted; ?>
        </div>
      <?php endif; ?>

      <?php print render($content['comments']); ?>

    <?php endif; ?>

  </div>
</div>
