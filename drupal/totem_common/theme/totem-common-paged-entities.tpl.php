<?php
/**
 * @file
 * totem-common-paged-entities.tpl.php
 */
?>

<div class="pager-group">
  <div class="pager-data">
    <?php print render($query->results); ?>
    <div class="clearfix"></div>
  </div>
  <?php print render($query->pager); ?>
</div>
