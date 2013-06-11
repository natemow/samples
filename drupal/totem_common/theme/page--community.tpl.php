<?php
/**
 * @file
 * page--community.tpl.php
 */
?>

<?php if ($messages): ?>
  <div id="messages"><?php print $messages; ?></div>
<?php endif; ?>

<div<?php print $attributes; ?>>
  <?php if (isset($page['header'])) : ?>
    <?php print render($page['header']); ?>
  <?php endif; ?>

  <?php if (isset($page['content'])): ?>
    <?php if (!$is_community_tab): ?>

      <?php print render($page['content']); ?>

    <?php else: ?>

      <div class="section section-content" id="section-content">
        <div class="zone-wrapper zone-preface-wrapper clearfix" id="zone-preface-wrapper">
          <div class="zone zone-preface clearfix container-12" id="zone-preface">
            <?php print render($page['content']['preface']['content_top']); ?>
          </div>
        </div>
        <div class="zone-wrapper zone-content-wrapper clearfix" id="zone-content-wrapper">
          <div class="zone zone-content clearfix container-12 default-wrapper" id="zone-content">
            <div class="community-tab-header">

              <?php if (!empty($blocks_action)): ?>
              <div class="grid-2<?php print $wrapper_css_content; ?>">
                <div class="hd">
                  <h2 class="page-header"><?php print $title; ?></h2>
                </div>
              </div>
              <div class="grid-10<?php print $wrapper_css_sidebar_first; ?>">
                <?php print render($blocks_action); ?>
              </div>
              <?php else: ?>
                <div class="hd">
                  <h2 class="page-header"><?php print $title; ?></h2>
                </div>
              <?php endif; ?>

              <div class="clearfix"></div>
            </div>
            <div class="default-wrapper-content">
              <?php print render($page['content']['content']['content']); ?>
              <?php if (!empty($page['content']['content']['sidebar_first'])): ?>
                <?php print render($page['content']['content']['sidebar_first']); ?>
              <?php endif; ?>
              <div class="clearfix"></div>
            </div>
          </div>
        </div>
      </div>

    <?php endif; ?>
  <?php endif; ?>

  <?php if (isset($page['footer'])) : ?>
    <?php print render($page['footer']); ?>
  <?php endif; ?>
</div>
