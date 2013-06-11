<?php
/**
 * @file
 * totem-common-page-communities.tpl.php
 */
?>

<div class="pager-group">
  <table id="communities-list" class="pager-data">
    <thead>
      <th class="name"><?php print t("Name"); ?></th>
      <th class="stat-col members"><?php print t("Members"); ?></th>
      <th class="stat-col media"><?php print t("Media"); ?></th>
      <th class="stat-col discussions"><?php print t("Discussions"); ?></th>
    </thead>
    <?php
    // Set counter for striping classes.
    $i = 0;
    // (Per-page is set in totem_common_page_content_list().)
    if (PAGE_SIZE_LISTS_PAGE % 2 == 1 && isset($_GET['page'])):
      $i = ((int) $_GET['page'] % 2 ? 1 : 0);
    endif;

    foreach ($query->results as $cnid => $cnode_build):
      $pager_entity_class = ($i % 2 ? 'odd' : 'even');
    ?>
    <?php
    // Notes:
    // 1) Each .pager-entity must be a direct child of .pager-data
    // for AJAX paging to work correctly.
    // 2) Attempting to put each node's data in a single row (for sake of
    // semantics and AJAX paging) yet display the last cell containing
    // node body in a "separate row" caused many CSS difficulties.
    // Thus: we use two rows per node to make formatting easy, but group
    // them with <tbody> elements for semantics and to allow AJAX paging
    // to work (http://www.w3.org/TR/html401/struct/tables.html#edef-TBODY).
    ?>
    <tbody class="pager-entity <?php print $pager_entity_class; ?>">
      <tr>
        <td class="name">
          <span class="community-expand">+</span>
          <?php
          // Body is printed further below.
          hide($cnode_build['body']);
          // Image is not shown in this view.
          hide($cnode_build['field_image']);
          // Teaser is left as only the linked title now.
          print render($cnode_build);
          ?>
        </td>
        <td class="stat-col members">
          <?php print $counts[$cnid]['members']; ?>
        </td>
        <td class="stat-col media">
          <?php print $counts[$cnid]['media']; ?>
        </td>
        <td class="stat-col discussions">
          <?php print $counts[$cnid]['discussions']; ?>
        </td>
      </tr>

      <tr>
        <td colspan="4" class="description">
          <?php
          // jQuery slide effects appear not to work correctly on
          // table-related elements, so provide a <div> wrapper.
          ?>
          <div class="details-wrapper">
            <?php print render($cnode_build['body']); ?>
            <div class="community-actions">
              <?php
              // Pass this community node as context, since the block finds
              // community via _totem_common_get_community_context_node()
              // which doesn't work in this case.
              $join = _totem_common_embed_block('totem_user', 'button_add_user', $cnode_build['#node']);
              // Remove block markup (meh).
              unset($join['totem_user_button_add_user']['#theme_wrappers']);
              print render($join);
              ?>

              <a href="<?php print $cnode_build['#node_url']; ?>" <?php print drupal_attributes($cnode_build['#node_url_attributes']); ?>>More details</a>
            </div>
          </div>
        </td>
      </tr>
    </tbody>
    <?php
    $i++;
    endforeach;
    ?>
  </table>
  <?php print render($query->pager); ?>
</div>
