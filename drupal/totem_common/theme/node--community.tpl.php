<?php
/**
 * @file
 * node--community.tpl.php
 */
?>

<?php if ($view_mode == 'teaser'): ?>

  <div<?php print $attributes; ?>>
    <div<?php print $content_attributes; ?>>
      <div class="header">
        <?php
        // Check #printed flag so we respect any hide() calls.
        if (empty($content['field_image']['#printed']) && !empty($content['field_image'][0]['sized_images'])):
        ?>
          <a href="<?php print $node_url; ?>" <?php print drupal_attributes($node_url_attributes); ?> class="community-image"><?php print $content['field_image'][0]['sized_images']['community_image']; ?></a>
        <?php endif; ?>
        <?php if ($title): ?>
          <?php print render($title_prefix); ?>
          <h3<?php print $title_attributes; ?> class="community-title">
            <a href="<?php print $node_url; ?>" <?php print drupal_attributes($node_url_attributes); ?>><?php print $title; ?></a>
          </h3>
          <?php print render($title_suffix); ?>
        <?php endif; ?>
        <?php if ($display_submitted): ?>
          <div class="submitted clearfix">
            <?php print $user_picture; ?>
            <div>Submitted by <?php print $name; ?><br />on <?php print $date; ?></div>
          </div>
        <?php endif; ?>

        <?php if (!empty($content['intro_text'])): ?>
          <div class="intro-text"><?php print render($content['intro_text']); ?></div>
        <?php endif; ?>
      </div>
    </div>
  </div>

<?php endif;
