#!/bin/bash

# Function to ensure that target cloudfiles dir is mounted.
# http://sandeepsidhu.wordpress.com/2011/03/07/mounting-cloud-files-using-cloudfuse-into-ubuntu-10-10-v2/
function mountFS() {
    mp=${1}
    sd=${2:-"false"}
    if grep -q "[[:space:]]${mp}[[:space:]]" /proc/mounts; then
        echo "${mp} already mounted"
    else
        umount -f -l ${mp}
        if [ ${sd} == "true" ]
         then
            sudo mount ${mp}
        else
            mount ${mp}
        fi
    fi
}

mountFS /mnt/cloudfiles


# --dry-run

# rsync theme's "css/img" dir.
LOG=/var/www/html/drupal/sites/memories/logs/rsync_cloudfiles_theme.log
SOURCE=/var/www/html/drupal/sites/memories/themes/memories_ui/css/img
DESTINATION=/mnt/cloudfiles/memories-production/sites/default/themes/memories_ui/css

mv -f "$LOG.tmp" $LOG
> "$LOG.tmp"
nohup rsync -rzvOX --delete --delete-delay --force --ignore-errors --whole-file $SOURCE $DESTINATION > "$LOG.tmp" &


# rsync "files/js" dir.
LOG=/var/www/html/drupal/sites/memories/logs/rsync_cloudfiles_files_js.log
SOURCE=/var/www/html/drupal/sites/memories/files/js
DESTINATION=/mnt/cloudfiles/memories-production/files

mv -f "$LOG.tmp" $LOG
> "$LOG.tmp"
nohup rsync -rzvOX --delete --delete-delay --force --ignore-errors --whole-file $SOURCE $DESTINATION > "$LOG.tmp" && rm -f "$SOURCE/aggregate.js" &


# rsync "files/css" dir.
LOG=/var/www/html/drupal/sites/memories/logs/rsync_cloudfiles_files_css.log
SOURCE=/var/www/html/drupal/sites/memories/files/css
DESTINATION=/mnt/cloudfiles/memories-production/files

mv -f "$LOG.tmp" $LOG
> "$LOG.tmp"
nohup rsync -rzvOX --delete --delete-delay --force --ignore-errors --whole-file $SOURCE $DESTINATION > "$LOG.tmp" && rm -f "$SOURCE/aggregate.css" &


# rsync "files" dir.
LOG=/var/www/html/drupal/sites/memories/logs/rsync_cloudfiles_files.log
SOURCE=/var/www/html/drupal/sites/memories/files
DESTINATION=/mnt/cloudfiles/memories-production

mv -f "$LOG.tmp" $LOG
> "$LOG.tmp"
nohup rsync -rzvOX --delete --delete-delay --force --ignore-errors --whole-file --exclude-from="/root/scripts/rsync_memories/rsync_exclude.txt" $SOURCE $DESTINATION > "$LOG.tmp" &

