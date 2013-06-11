<?php
/**
 * @file
 * taxonomy-term--tags--totem.tpl.php
 */
?>

<a href="<?php print $term_url; ?>" class="<?php print $classes; ?>"><span><?php print $term_name; ?></span></a>
<?php print render($content);
