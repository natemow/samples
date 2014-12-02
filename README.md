# Publication Assistant 
Publication assistant is a collection of tools that our internal staff use on a weekly basis to support online publication of our journals and news content. Essentially they are a set of utility scripts that extract data from our central XML collection, perform some simple XSL transforms and repurpose it for: RSS Feeds and other templatized output for delivery to Highwire environment, Email alerts that are pushed to the Eloqua service, and a database operation to push metadata to our internal Drupal comment system (comments.sciencemag.org).

## Current Status
Written in CodeIgniter, project was in the process of migrating from v2.1.2 to v2.1.3, re-architecting and including some new functionality. Work is only partially completed. Some functionality is complete, some is missing, there is some legacy stuff left over that no longer needs to be there.

### Schedule:

* Start work: 9/15
* Hard status update: 9/19
* Delivery: items 1 - 3 by 10/3, 4 - 5 by 10/10

***

### App Issues:

1. "Build Selected" takes considerably longer to process than "Schedule" [Note I've disabled "Schedule" in UI for the moment.]
2. **UPDATE: It seems like custom subjects are being sent, but it is using what was last-saved, not the most current one.** [Custom subject lines are not being sent to eloqua when "Schedule" button is pressed. I do see the correct subject line in the database field, and in the UI, but still see "Latest News and Headlines Today." in what I receive from Eloqua. Correct subject does seem to appear there when "Build Selected" is pressed
3. On the 'Science' Tab "Current Issue - RSS" returns: "error while processing forms This is a server error. Please notify a developer for support.
4. Cron scheduler not working.
5. Build selected should display a more meaningul dialog than "undefined ( news_sci-now-latest ): Success"
6. Need notes explaining how/where test emails are uploaded to Eloqua - how "SCI-TEST" prefix is 
7. UI takes quite a while to display the emails after page loads.
8. Noting we had a temporary issue where "Build Selected" stopped working. It is ok again now. Not clear why - possibly unusual characters in the RSS feed.

### UI Issues:
1. Bug: When I check a news email - the 'build' and 'scheduling' controls are never enabled. I have to select/unselect a random one on another screen.
2. When saving subject lines - display a single, consolidated growl dialog instead of multiple separate ones. 

***

## General Help
Must-have PHP:
* `php5-curl` `php5-xsl` `php-soap`
* If running gt PHP 5.3, `application/pm/third_party/email/EloquaServiceClient.php` *will* throw fatals if line 946 is not commented out. e.g.:
  <pre>
  Fatal error: Declaration of RetrieveResponseIterator::__construct() must be compatible with ResponseIterator::__construct() in /var/www/vhosts/aaas/CI-Publisher/ci.dev.2.1.3/application/pm/third_party/email/EloquaServiceClient.php on line 1018"
  </pre>
* All of `output` dir should be recursively writeable. (`chmod -R 0777 application/pm/output`)

Must-have debug tools:
* http://craig.is/writing/chrome-logger
* https://github.com/ccampbell/chromephp

## Deployment Notes (summary)

When moving to a new environment, note the following:

* update [..]/config/testing/pm_config.php
* update [..]/config/testing/database.php
* chmod -R [..]/output to 0766

Add tables to database (if not there already):

<pre>
# Create `sci_meta.eloqua` deployment log table:
CREATE TABLE `eloqua` (
  `JournalId` varchar(20) NOT NULL,
  `AlertId` varchar(20) NOT NULL,
  `DeploymentId` int(11) NOT NULL,
  `DeploymentDate` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `EmailName` varchar(255) NOT NULL,
  PRIMARY KEY (`DeploymentId`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='latin1_swedish_ci'

CREATE TABLE `variable` (
  `name` varchar(128) NOT NULL,
  `value` longtext NOT NULL,
  PRIMARY KEY  (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='latin1_swedish_ci';
</pre>

Set Up CRONTAB for scheduling, on local machine (edit URL as needed):
<pre>
# Set 1 crontab entry, running every 5m, (`crontab -e`):
*/5 * * * * wget -O - http://edprod01.aaas.org/ci.dev.2.1.3/index.php/alertMaster/scheduleEloquaEmailCron >/dev/null 2>&1
</pre>


# TODOs
## v2.1.2
1. `status: DONE PER v2.1.3 #3 below.` Retain email alerts "Review and Send eAlerts"
  1. Merge to "Emails Newsletters and Feeds" in v2.1.3
1. v2.1.2 should go away entirely by the time this TODO list is completed.

## v2.1.3

1. TOP PRIORITY Confirm that Eloqua integration works, possible need to re-config stuff.
  1. `status: DONE` "Preview" btn in "Emails Newsletters and Feeds" allows visible to Eloqua; goal is "Send to Eloqua" interface on main "Emails Newsletters and Feeds". Socha to follow-up w/Martyn when we have a handle on it, M to make decisions on what it should do. This all works in v2.1.2.
  1. `status: DONE` Need to make scheduler utility + cron task to automatically post to Eloqua; Eloqua takes over at that point and sends. Applicable to "Science", "Science News", "Science Signaling", "Science Translational Medicine" tabs.
    <pre>
    (03:05:07 PM) natemow@gmail.com/F207A976: k. per "Need to make scheduler utility + cron task to automatically post to Eloqua" -- where do you see this living?
    (03:05:45 PM) natemow@gmail.com/F207A976: i've found half-baked code to push stuff to eloqua in v3 but nothing to trigger it
    (03:05:46 PM) Martyn Green: I guess on the same server as this app - it's inside our firewall
    (03:06:13 PM) natemow@gmail.com/F207A976: "schedule utility"? -- you don't want to control date/time directly?
    (03:06:31 PM) natemow@gmail.com/F207A976: i'll totally go write a new bash script if that's all your after
    (03:06:46 PM) Martyn Green: yeah doesn't need to be anything flashy.
    (03:07:16 PM) Martyn Green: they keep tweaking times for individual emails so as long as its' easy for someone with half a brain to edit it'll be fine for noo
    (03:07:43 PM) natemow@gmail.com/F207A976: okay, so this still needs to happen first tho right? "goal is "Send to Eloqua" interface on main "Emails Newsletters and Feeds""
    (03:08:04 PM) natemow@gmail.com/F207A976: you need some way to post HTML from CI to Eloqua from this dashboard yes?
    (03:08:41 PM) Martyn Green: yus... get the emails up to eloqua. Once that happens we can pretty much sever v.2 and focus everything into v.3
    (03:09:13 PM) Martyn Green: yes. v.2 is doing that.
    (03:09:38 PM) natemow@gmail.com/F207A976: okay. so if i had a button that basically ran through and force-rendered exactly what Preview does, all that HTML gets written to output/whatever dirs, then bash script trolls those and pushes up to Eloqua?
    (03:10:11 PM) Martyn Green: there's an eloqua api that the thing talked to before I think
    (03:10:25 PM) natemow@gmail.com/F207A976: or button also pushes to Eloqua and all bash script does is schedule a send date/time?
    (03:10:38 PM) natemow@gmail.com/F207A976: yes, looking Eloqua API now -- there's a lot of shit there
    (03:13:01 PM) Martyn Green: tell me if this sounds nuts.... CI has a process that builds one or more selected emails - writes html the specified location and triggers eloqua API to either just upload, or upload and send the newsletter. Then we have a bash/CRON thinger that sits there and whenever triggered will activate the same CI event. That way they can send the things manually, or trigger it via the CRON/Bash thing.
    (03:13:28 PM) Martyn Green: \application\pm\third_party\email
    (03:13:40 PM) Martyn Green: that's the library in v.2
    (03:13:44 PM) natemow@gmail.com/F207A976: right
    (03:14:16 PM) natemow@gmail.com/F207A976: sounds fine -- really its almost exactly what's in v2 now, but with an exposed endpoint so cron can also trigger it. yes?
    (03:14:36 PM) Martyn Green: btw - "upload and send" is mildly scary as people could inadvertantly email a million people!
    (03:15:23 PM) Martyn Green: sounds about right... if we can have an extra UI step to stop muthas sending this thing when they drop someting on the keyboard that would be ace.
    (03:15:39 PM) Martyn Green: you have nailed it, sir...
    (03:15:50 PM) natemow@gmail.com/F207A976: heh. no to unwire it all in v2...
    (03:15:57 PM) natemow@gmail.com/F207A976: this app is...dense
    (03:16:22 PM) Martyn Green: yes... that's why I never dived in deep! you are the man for the job!
    </pre>

    <pre>
    # Eloqua GUI credentials:
    https://login.eloqua.com
    Company: AAAS
    Username: Nate.Mow
    Password: Tester123
    
    # Client POC for Eloqua:
    Elizabeth Sattler
    Marketing Manager
    esattler@aaas.org, 202-326-6669

    # Live mode:
    Communicate > Email Marketing > Email 
    > 3 Journal Content Alerts > 35 ScienceNow Daily Alert > 2014

    # Testing mode:
    Communicate > Email Marketing > Email 
    > Unified > Test_Eloqua_Automation

      *search for "SCI-TEST"* (written to group 59 per ENVIRONMENT == 'testing') 
      @see `application/pm/third_party/email/email_functions.php()::createHTMLEmail()`.
    </pre>
    Note: previous `global $test_mode;` var has been removed in favor of `/index.php (ENVIRONMENT == 'testing');` check.

    <pre>
    # DEPLOYMENT TODO -- create `sci_meta.eloqua` deployment log table:
    CREATE TABLE `eloqua` (
      `JournalId` varchar(20) NOT NULL,
      `AlertId` varchar(20) NOT NULL,
      `DeploymentId` int(11) NOT NULL,
      `DeploymentDate` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
      `EmailName` varchar(255) NOT NULL,
      PRIMARY KEY (`DeploymentId`)
    ) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='latin1_swedish_ci'

    # DEPLOYMENT TODO -- set 1 crontab entry, running every 5m, to handle all scheduling (`crontab -e`):
    */5 * * * * wget -O - http://local.aaas-ci3/index.php/alertMaster/scheduleEloquaEmailCron >/dev/null 2>&1
    </pre>

    `status: 9/17` Had a call with Martyn; we've verified that the "Make Selected" button does write to Eloqua and also writes RSS feed renderings to `application/pm/output/rss` dir. As such, the `Put/Put Test/Put Live` widget can go away entirely now. Per the Scheduler Utility task defined above, we're going to instead create a one-off cron script that performs the same actions as "Make Selected" AND ALSO queues up the actual send schedule in Eloqua @see `Put Live` underbelly, particularly `email_functions.php::scheduleDeployment()`.

    `status: 9/20` Migrated hard-coded Eloqua list IDs from `email_functions.php::sendXYZ()` testing functions to corresponding `alert_*_config.php::eloqua::list_id` arrays; corrected checkbox alert key mapping so that configs could be properly loaded to new `alertMaster/scheduleEloquaEmail()` endpoint. Added new "Schedule Eloqua Newsletters" button to hit this endpoint; cron endpoint started at `alertMaster/scheduleEloquaEmailCron()` (http://local.aaas-ci3/index.php/alertMaster/scheduleEloquaEmailCron) -- this endpoint will (A) Compile all alerts and send to Eloqua and (B) Call the Eloqua scheduling routine.

    `status: 9/23` All scheduling now handled via `alert_*_config.php` files: `$config['alerts'][JOURNAL_ID][ALERT_ID]['eloqua']['schedule']['test' OR 'live']` where test/live is controlled by '/index.php ENVIRONMENT' constant.

    `status: 9/30` Daily and Weekly News feed parsing corrected; now able to write messages to Eloqua.
  1. `status: 10/5 DONE` Add Eloqua custom subject line to emails.
    <pre>
    (06:30:54 PM) natemow@gmail.com/28DFBF36: real quick: "Add Eloqua custom subject line to emails." -- are you looking for an actual interface here in v3? or just a check on a custom config val?
    (06:31:43 PM) natemow@gmail.com/28DFBF36: i think we'll need to dig in to Eloqua API update email bits to make that happen (which it doesn't look like C had done thus far)
    (06:32:21 PM) Martyn Green: This will change for each email... So it needs to be easy for a non tech to do. But I can check Monday who does this an how.
    (06:32:30 PM) natemow@gmail.com/28DFBF36: k
    </pre>
    <pre>
    # DEPLOYMENT TODO -- create `sci_meta.variable` table:
    CREATE TABLE `variable` (
      `name` varchar(128) NOT NULL,
      `value` longtext NOT NULL,
      PRIMARY KEY (`name`)
    ) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='latin1_swedish_ci'
    </pre>
    1. `status: 10/2 DONE` Port subject line tokenization from v2 to v3.
    1. `status: 10/5 DONE` Persistent storage of Eloqua subject lines now in place, stored values present in GUI and, when defined, values now override what is otherwise hardcoded in to the `alert_*_config.php` Eloqua configuration. Values also persist to scheduled cron runs.
  1. `status 9/19: ON HOLD; ENRIQUE TO VERIFY THAT ELOQUA CAN ACTUALLY HANDLE THIS DIRECTLY INSTEAD.` From Martyn: "I've gone ahead and added the functionality to version CI.2 so that campaign strings are appended to the links in the emails. I added a "campaign" attribute to each of the alerts in the four config files and modified a controller and couple o' views to spit everything out." -- need to port this v3.
1. `status: IN PROGRESS, CURRENTLY WAITING ON FINAL RSS FEED TO BE DEPLOYED TO PROD; UI WORK IS COMPLETE.` Martyn to revamp email template, Socha to integrate to v2.1.3 @see pm/views/templates/alerts, pm/views/templates/header and determine how/where Martyn needs to implement, determine what customization of php vars is available to him i.e. $body $header.
  </pre>
  (12:40:44 PM) Martyn Green: Hey man... quick update on my end... I have a handle on the HTML template and think I can handle everything there. So #2 on the to do list is probably ok at this point. I have it working on v.2 and think it's the same templating model in v.3
  </pre>
1. `status: DONE PER 9/17 NOTE IN 1.ii ABOVE` "Emails Newsletters and Feeds" - missing Newsletter functionality; build this to completion
  1. @see `config/[env]/*feed.php` (Feeds), `config/[env]/pm_config.php`
  <pre>
  (12:53:14 PM) natemow@gmail.com/9B6A9BA1: for #3 -- "status: DONE PER 9/17 NOTE IN 1.ii ABOVE" -- does that sound right? our TODO notes aren't super-clear to me at this point.
  (12:54:21 PM) Martyn Green: 1.ii says still in progress with the scheduler...
  (12:55:11 PM) natemow@gmail.com/9B6A9BA1: see 1.ii `status: 9/17`
  (12:55:13 PM) Martyn Green: but I think that the UI part of the app is done - right? so it depends whether you're counting the scheduler as a separate TODO
  (12:55:37 PM) natemow@gmail.com/9B6A9BA1: right, the UI part is done -- that's what the #3 note is ref'ing
  (12:55:48 PM) natemow@gmail.com/9B6A9BA1: the cron thing is sort of separate
  (12:55:59 PM) natemow@gmail.com/9B6A9BA1: tho code is shared obviously
  (12:56:37 PM) Martyn Green: yah... #3 is done, that's refering to how only the "Feeds" part of the UI in v.3 was working.
  (12:56:52 PM) Martyn Green: but now it's armed and fully operational
  (12:56:56 PM) natemow@gmail.com/9B6A9BA1: cool
  </pre>
1. `status: 10/5 PENDING` "Fragment Generator" half-baked in v3; make this work. The fragments are output to file, staff copies to Highwire. Identify other fragment usages i.e. Newsletter builds.
  1. @see config/[env]/fragments.php
  1. Fix `->as_html()` func; review `article.php` model. @see ~line 120. @see `simple.xsl` view at pm/view/temlates/sfragments/sci_twis.php
  1. Once fragments are written to network drive, look at weird `section` tag and also debug `img` output; something is choking there.
  1. Generator should be able to target any channel
	Science (sci) Signaling (sig) Science Translational Medicine (stm) -- all come from XML
	News (news) -- comes from RSS feed; presently no configurable data source @see `alert_news_config.php` in v2.1.3 `news_alert_config.php` in v2.1.2
	Migrate conf above to master `pm_config.php`

1. `status: 10/5 PENDING` "Ad Content Manager" and "Editorial Content Manager" - do a code review and make sure that functionality is not duplicated; if it is, merge to 1 code base. Review inline TODO notes and resolve as needed @see `application/pm/controllers/alertMaster.php`
