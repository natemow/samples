<?php
/**
 * @file
 * totem-common-node-community-box.tpl.php
 */
?>

<?php if (!empty($query->results)): ?>

  <div class="hd clearfix">
    <h2><?php print $title_link; ?></h2>
    <?php if ($hook == 'user_community_community'): ?>
      <?php if ($is_own_profile): ?>
        <?php print render($header_block); ?>
      <?php endif; ?>
    <?php else: ?>
      <?php print render($header_block); ?>
    <?php endif; ?>
  </div>
  <div class="bd clearfix">
    <div class="pager-group">
      <div class="pager-data">
        <?php print render($query->results); ?>
      </div>
      <?php if ($show_pager): ?>
        <?php print render($query->pager); ?>
      <?php else: ?>
        <?php print $more_link; ?>
      <?php endif; ?>
    </div>
  </div>

<?php else: ?>

  <?php if ($hook == 'user_community_community'): ?>

    <div class="hd clearfix">
      <h2><?php print $title_link; ?></h2>
    </div>
    <div class="bd clearfix">
      <div class="welcome">
      <?php if ($is_own_profile): ?>
        <?php print t('<p>You currently have no @community.</p>', array('@community' => t('Communities'))); ?>
        <?php print render($header_block); ?>
      <?php else: ?>
        <?php print t('<p>This member currently has no @community.</p>', array('@community' => t('Communities'))); ?>
      <?php endif; ?>
      </div>
    </div>

  <?php elseif ($hook == 'user_community_recent'): ?>

    <div class="hd clearfix">
      <h2><?php print $title_link; ?></h2>
      <?php if ($is_own_profile): ?>
        <?php print render($header_block); ?>
      <?php endif; ?>
    </div>
    <div class="bd clearfix">
      <div class="activity-welcome">
        <h3>Keep track of everything new.</h3>
        <p>
        <?php if ($is_own_profile): ?>
          <?php print t("As you create and join @community,<br /> you can keep track of everyone's activities here.", array('@community' => t('Communities'))); ?>
        <?php else: ?>
          <?php print t("When this member creates or joins @community,<br /> you can keep track of their activity here.", array('@community' => t('Communities'))); ?>
        <?php endif; ?>
        </p>
        <p class="activity-sample clearfix">
          <?php print $account->content['images']['user_thumb']; ?>
          <?php print render($account->content['name']) . ' ' . t('joined @site_name - Welcome!', array('@site_name' => variable_get('site_name'))); ?>
        </p>
      </div>
    </div>

  <?php endif; ?>

<?php endif;
