<?php
/**
 * @file
 * totem_common.features.user_permission.inc
 */

/**
 * Implements hook_user_default_permissions().
 */
function totem_common_user_default_permissions() {
  $permissions = array();

  // Exported permission: access community node view.
  $permissions['access community node view'] = array(
    'name' => 'access community node view',
    'roles' => array(
      0 => 'administrator',
      1 => 'anonymous user',
      2 => 'authenticated user',
    ),
    'module' => 'internal_nodes',
  );

  // Exported permission: access contextual links.
  $permissions['access contextual links'] = array(
    'name' => 'access contextual links',
    'roles' => array(
      0 => 'administrator',
      1 => 'authenticated user',
    ),
    'module' => 'contextual',
  );

  // Exported permission: access page node view.
  $permissions['access page node view'] = array(
    'name' => 'access page node view',
    'roles' => array(
      0 => 'administrator',
      1 => 'anonymous user',
      2 => 'authenticated user',
    ),
    'module' => 'internal_nodes',
  );

  // Exported permission: create community content.
  $permissions['create community content'] = array(
    'name' => 'create community content',
    'roles' => array(
      0 => 'administrator',
      1 => 'authenticated user',
    ),
    'module' => 'node',
  );

  // Exported permission: delete own community content.
  $permissions['delete own community content'] = array(
    'name' => 'delete own community content',
    'roles' => array(
      0 => 'administrator',
      1 => 'authenticated user',
    ),
    'module' => 'node',
  );

  // Exported permission: edit own community content.
  $permissions['edit own community content'] = array(
    'name' => 'edit own community content',
    'roles' => array(
      0 => 'administrator',
      1 => 'authenticated user',
    ),
    'module' => 'node',
  );

  // Exported permission: view own unpublished content.
  $permissions['view own unpublished content'] = array(
    'name' => 'view own unpublished content',
    'roles' => array(
      0 => 'administrator',
      1 => 'authenticated user',
    ),
    'module' => 'node',
  );

  return $permissions;
}
