<?php

/**
 * @file
 * page.tpl.php
 */
?>
  <div id="page">
    <header id="header">
      <div class="row">
        <div class="columns small-6 large-6">
          <?php if ($site_name): ?>
            <?php if ($title): ?>
              <div class="brand"><a href="<?php print $front_page; ?>" title="<?php print t('Home'); ?>" rel="home"><span class="element-invisible"><?php print $site_name; ?></span></a></div>
            <?php else: ?>
              <h1 class="brand"><a href="<?php print $front_page; ?>" title="<?php print t('Home'); ?>" rel="home"><span class="element-invisible"><?php print $site_name; ?></span></a></h1>
            <?php endif; ?>
          <?php endif; ?>
          <?php if ($logo): ?>
            <a href="<?php print $front_page; ?>" title="<?php print t('Home'); ?>" rel="home" id="logo">
              <img src="<?php print $logo; ?>" alt="<?php print t('Home'); ?>" />
            </a>
          <?php endif; ?>
          <?php if ($site_slogan): ?>
            <?php print $site_slogan; ?>
          <?php endif; ?>
        </div>
        <div class="columns small-6 large-6">
          <nav class="row">
            <?php if ($secondary_menu): ?>
            <div class="columns small-12 large-12">
              <?php
              print theme('links__secondary_menu', array(
                'links' => $secondary_menu,
                'attributes' => array(
                  'class' => array('menu', 'right'),
                ),
              ));
              ?>
            </div>
            <?php endif; ?>
            <?php if ($main_menu): ?>
            <div class="columns small-12 large-12">
              <?php
              print theme('links__system_main_menu', array(
                'links' => $main_menu,
                'attributes' => array(
                  'class' => array('menu', 'right'),
                ),
                'heading' => 'Main menu',
              ));
              ?>
            </div>
            <?php endif; ?>
          </nav>
          <?php print render($page['header']); ?>
        </div>
      </div>
    </header>
    <div id="content">
      <div class="row">
        <section class="columns <?php print $page['grid_classes_content']; ?>">
          <h2 class="element-invisible">Main content</h2>
          <a id="main-content"></a>
          <?php if ($messages): ?>
            <?php print $messages; ?>
          <?php endif; ?>
          <?php if ($page['featured']): ?>
            <?php print render($page['featured']); ?>
          <?php endif; ?>
          <?php print render($title_prefix); ?>
          <?php if ($title): ?><h1><?php print $title; ?></h1><?php endif; ?>
          <?php print render($title_suffix); ?>
          <?php if ($tabs = render($tabs)): ?><div class="tabs"><?php print $tabs; ?></div><?php endif; ?>
          <?php print render($page['help']); ?>
          <?php if ($action_links = render($action_links)): ?><ul class="action-links"><?php print $action_links; ?></ul><?php endif; ?>
          <?php print render($page['content']); ?>
        </section>
        <?php if ($page['sidebar_first']): ?>
        <section class="columns <?php print $page['grid_classes_sidebar_first']; ?>">
          <h2 class="element-invisible">Secondary content</h2>
        <?php print render($page['sidebar_first']); ?>
        </section>
        <?php endif; ?>
        <?php if ($page['sidebar_second']): ?>
        <section class="columns <?php print $page['grid_classes_sidebar_second']; ?>">
          <h2 class="element-invisible">Tertiary content</h2>
        <?php print render($page['sidebar_second']); ?>
        </section>
        <?php endif; ?>
      </div>
    </div>
    <footer id="footer">
      <h2 class="element-invisible">Footer</h2>
      <div class="row">
        <div class="columns small-12 large-12">
          <?php if ($page['footer']): ?>
            <?php print render($page['footer']); ?>
          <?php endif; ?>
          <p>&copy; <?php print date('Y'); ?> Your Organization. All rights reserved.</p>
        </div>
      </div>
    </footer>
  </div>
