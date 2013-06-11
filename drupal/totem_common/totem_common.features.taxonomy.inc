<?php
/**
 * @file
 * totem_common.features.taxonomy.inc
 */

/**
 * Implements hook_taxonomy_default_vocabularies().
 */
function totem_common_taxonomy_default_vocabularies() {
  return array(
    'tags' => array(
      'name' => 'Tags - freeform',
      'machine_name' => 'tags',
      'description' => 'Site-wide freeform tag list.',
      'hierarchy' => '0',
      'module' => 'taxonomy',
      'weight' => '0',
    ),
    'tags_fixed' => array(
      'name' => 'Tags - fixed',
      'machine_name' => 'tags_fixed',
      'description' => 'Fixed tag list.',
      'hierarchy' => '0',
      'module' => 'taxonomy',
      'weight' => '0',
    ),
  );
}
