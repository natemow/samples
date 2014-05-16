#!/bin/bash

MYSQL_HOST="localhost:3306"
DRUPAL_VERSION="7.26"
DRUPAL_ARCHIVE="http://ftp.drupal.org/files/projects/drupal-$DRUPAL_VERSION.tar.gz"
DRUPAL_ARCHIVE_PREFIX="drupal"
DRUPAL_PROFILE="standard"

echo ""
read -p "This script will delete the \"current\" symlink in $(pwd),
download a Drupal distro and install per your arguments; after installation,
the \"current\" symlink will be re-created and pointed to the distro instance.

Do you want to proceed (y/n)? " CONFIRM
echo ""
if [ $CONFIRM != "y" ]; then
  exit
fi

read -p "MySQL host:port (<Enter> for localhost:3306): " MYSQL_HOST_ALT
read -p "MySQL database: " MYSQL_DB
read -p "MySQL username: " MYSQL_USER_NAME
read -p "MySQL password: " MYSQL_USER_PASS
read -p "Drupal distro archive URL (<Enter> for $DRUPAL_VERSION default): " DRUPAL_ARCHIVE_ALT
read -p "Drupal profile (machine name, <Enter> for $DRUPAL_VERSION default): " DRUPAL_PROFILE_ALT
read -p "Drupal site name: " DRUPAL_SITE_NAME
read -p "Drupal uid:1 username: " DRUPAL_USER_NAME
read -p "Drupal uid:1 password: " DRUPAL_USER_PASS
read -p "Drupal uid:1 mail: " DRUPAL_USER_MAIL
echo ""

if [ -n "$MYSQL_HOST_ALT" ]; then
  MYSQL_HOST=$MYSQL_HOST_ALT
fi

if [ -n "$DRUPAL_ARCHIVE_ALT" ]; then
  DRUPAL_ARCHIVE=$DRUPAL_ARCHIVE_ALT
fi

if [ -n "$DRUPAL_PROFILE_ALT" ]; then
  DRUPAL_PROFILE=$DRUPAL_PROFILE_ALT
  DRUPAL_ARCHIVE_PREFIX=$DRUPAL_PROFILE
fi

DRUPAL_DIRECTORY=$(pwd)"/$DRUPAL_PROFILE"

wget $DRUPAL_ARCHIVE
tar -xzf $DRUPAL_ARCHIVE_PREFIX-*
rm -rf $DRUPAL_ARCHIVE_PREFIX-*.tar.gz $DRUPAL_PROFILE current
mv $DRUPAL_ARCHIVE_PREFIX-* $DRUPAL_PROFILE
ln -s $DRUPAL_DIRECTORY docroot
cd docroot

drush site-install \
  $DRUPAL_PROFILE \
  --db-url="mysql://$MYSQL_USER_NAME:$MYSQL_USER_PASS@$MYSQL_HOST/$MYSQL_DB" \
  --locale=en \
  --account-mail="$DRUPAL_USER_MAIL" \
  --account-name="$DRUPAL_USER_NAME" \
  --account-pass="$DRUPAL_USER_PASS" \
  --site-mail="$DRUPAL_USER_MAIL" \
  --site-name="$DRUPAL_SITE_NAME"

drush user-password \
  $DRUPAL_USER_NAME \
  --password=$DRUPAL_USER_PASS

echo ""
echo "\"$DRUPAL_PROFILE\" profile installed successfully to $DRUPAL_DIRECTORY. A \"docroot\" symlink to the directory was also created."
echo ""
read -p "This script can also install common contrib mods and do some basic configuration.
Do you want to proceed (y/n)? " CONFIRM
echo ""
if [ $CONFIRM != "y" ]; then
  exit
fi

mkdir -p "$DRUPAL_DIRECTORY/sites/all/modules/contrib"
mkdir -p "$DRUPAL_DIRECTORY/sites/all/modules/custom"
mkdir -p "$DRUPAL_DIRECTORY/sites/all/libraries"
mkdir -p "$DRUPAL_DIRECTORY/sites/all/themes"
mkdir -p "$DRUPAL_DIRECTORY/sites/all/patches"

wget "http://download.cksource.com/CKEditor/CKEditor/CKEditor%204.3.4/ckeditor_4.3.4_standard.zip"
unzip *.zip -d "$DRUPAL_DIRECTORY/sites/all/libraries/"
rm -f *.zip

cd "$DRUPAL_DIRECTORY/sites/all/modules/custom"
drush pm-disable dashboard toolbar overlay color comment rdf shortcut help dblog
drush pm-uninstall dashboard toolbar overlay color comment rdf shortcut help dblog

drush dl admin_menu adminimal_admin_menu module_filter jquery_update devel ctools token views pathauto redirect entity eck entityreference features variable webform block_class markup mailsystem mimemail ckeditor imce imce_mkdir metatag opengraph_meta googleanalytics
drush pm-enable syslog stark adminimal_admin_menu module_filter jquery_update devel ctools token views views_ui pathauto entity eck entityreference features variable variable_admin webform block_class markup mimemail redirect ckeditor imce imce_mkdir metatag opengraph_meta googleanalytics

cd "$DRUPAL_DIRECTORY/sites/all/themes"
drush dl adminimal_theme

drush vset --yes jquery_update_compression_type "min"
drush vset --yes jquery_update_jquery_cdn "google"
drush vset --yes theme_default "stark"
drush vset --yes admin_theme "adminimal"

drush pm-disable bartik seven

drush cc all
