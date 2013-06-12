#!/bin/bash
 
# Usage:
# sudo /home/nate/scripts/interactive_drupal_site_export.sh /home/nate/k12 k12 k12_stage profiles/interactive/modules/features/interactive_blog,profiles/interactive/modules/features/interactive_vip,profiles/interactive/modules/features/interactive_seo,profiles/interactive/modules/features/interactive_google

# RESOURCE_DIR contains favicon.ico, client-centric sites.php copy, drush_archive_prep.sql.
RESOURCE_DIR=$1

# The drupal/sites directory to export.
SITE_DIR=$2

# MySQL connection params.
read -p "MySQL username: " MYSQL_USER
read -p "MySQL password: " MYSQL_PASS
MYSQL_DB=$3

# Comma-separated list of paths under drupal root to exclude from archive.
EXCLUDE_DIRS=$4

# Prep the tar path exclusion arg.
EXCLUDE_DIRS=${EXCLUDE_DIRS//,/$' '}
EXCLUDE_ARGS=$'--exclude=xhprof_html'
for path in $EXCLUDE_DIRS
do
  EXCLUDE_ARGS+=" --exclude=$path"
done


# Now do stuff...
echo "Changing to interactive_drupal root..."
cd /var/www/vhosts/interactiverequest.com/subdomains/interactive-drupal/httpdocs/drupal

echo "Prepping $MYSQL_DB for export..."
mysql -u $MYSQL_USER -p$MYSQL_PASS -h localhost $MYSQL_DB < "$RESOURCE_DIR/drush_archive_prep.sql"

echo "Backing up private sites.php, moving client-centric favicon.icon, sites.php files..."
mv -f "$RESOURCE_DIR/favicon.ico" ./
mv sites/sites.php ../
mv "$RESOURCE_DIR/sites.php" sites/

echo "Creating drush archive..."
drush archive-dump "$SITE_DIR" --preserve-symlinks --overwrite --tar-options="$EXCLUDE_ARGS" --destination="$RESOURCE_DIR/$SITE_DIR.tar.gz"

echo "Restoring private sites.php..."
mv -f ../sites.php sites/

echo "Done!"
chown nate "$RESOURCE_DIR/$SITE_DIR.tar.gz"
