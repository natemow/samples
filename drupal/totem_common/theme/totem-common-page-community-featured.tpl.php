<?php
/**
 * @file
 * totem-common-page-community-featured.tpl.php
 */
?>

<div class="main-view">
  <?php foreach ($query_full->results as $nid => $entity): ?>
    <div class="clearfix featured node-community-<?php print $entity['#node']->nid; ?>">
      <?php if (!empty($entity['link_image'])): ?>
        <div class="image"><?php print $entity['link_image']; ?></div>
      <?php endif; ?>
      <div class="feature">
        <h2><?php print $entity['link_title']; ?></h2>
        <?php if (isset($query_teaser->results[$nid]['body'])): ?>
          <div class="intro">
            <?php print render($query_teaser->results[$nid]['body']); ?>
          </div>
        <?php endif; ?>
        <?php print $entity['link_button']; ?>
      </div>
    </div>
  <?php endforeach; ?>
</div>
<div class="more-communities">
  <h2>More Communities</h2>
  <div class="container">
    <ul class="carousel">
    <?php foreach ($query_teaser->results as $entity): ?>
      <li class="slide"><?php print render($entity); ?></li>
    <?php endforeach; ?>
    </ul>
  </div>
  <a class="nav" id="prev_btn" href="#"></a>
  <a class="nav" id="next_btn" href="#"></a>
</div>
