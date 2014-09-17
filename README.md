# Publication Assistant 
Publication assistant is a collection of tools that our internal staff use on a weekly basis to support online publication of our journals and news content. Essentially they are a set of utility scripts that extract data from our central XML collection, perform some simple XSL transforms and repurpose it for: RSS Feeds and other templatized output for delivery to Highwire environment, Email alerts that are pushed to the Eloqua service, and a database operation to push metadata to our internal Drupal comment system (comments.sciencemag.org).

## Current Status
Written in CodeIgniter, project was in the process of migrating from v2.1.2 to v2.1.3, re-architecting and including some new functionality. Work is only partially completed. Some functionality is complete, some is missing, there is some legacy stuff left over that no longer needs to be there.

Schedule:

* Start work: 9/15
* Hard status update: 9/19
* Deliver by: 9/26

## General Help
Must-have PHP:
* `php5-curl` `php5-xsl` `php-soap`
* If running gt PHP 5.3, `application/pm/third_party/email/EloquaServiceClient.php` *will* throw fatals if line 946 is not commented out. e.g.:
  <pre>
  Fatal error: Declaration of RetrieveResponseIterator::__construct() must be compatible with ResponseIterator::__construct() in /var/www/vhosts/aaas/CI-Publisher/ci.dev.2.1.3/application/pm/third_party/email/EloquaServiceClient.php on line 1018"
  </pre>

Must-have debug tools:
* http://craig.is/writing/chrome-logger
* https://github.com/ccampbell/chromephp

# TODOs
## v2.1.2
1. Retain email alerts "Review and Send eAlerts"
  1. Merge to "Emails Newsletters and Feeds" in v2.1.3
1. v2.1.2 should go away entirely by the time this TODO list is completed.

## v2.1.3

1. TOP PRIORITY Confirm that Eloqua integration works, possible need to re-config stuff.
  1. `status: DONE` "Preview" btn in "Emails Newsletters and Feeds" allows visible to Eloqua; goal is "Send to Eloqua" interface on main "Emails Newsletters and Feeds". Socha to follow-up w/Martyn when we have a handle on it, M to make decisions on what it should do. This all works in v2.1.2.
  1. `status: IN PROGRESS` Need to make scheduler utility + cron task to automatically post to Eloqua; Eloqua takes over at that point and sends. Applicable to "Science", "Science News", "Science Signaling", "Science Translational Medicine" tabs.
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
    </pre>

    Live mode: Communicate > Email Marketing > Email > 3 Journal Content Alerts > 35 ScienceNow Daily Alert > 2014

    Testing mode: Communicate > Email Marketing > Email > *search for "SCI-TEST"* (written to group 59 per `email_functions.php $test_mode = true;`) @see `ci.dev.2.1.3/application/pm/third_party/email/email_functions.php()::createHTMLEmail()` also note the `$test_mode` global var.

    `status: 9/17` Had a call with Martyn; we've verified that the "Make Selected" button does write to Eloqua and also writes RSS feed renderings to `application/pm/output/rss` dir. As such, the `Put/Put Test/Put Live` widget can go away entirely now. Per the Scheduler Utility task defined above, we're going to instead create a one-off cron script that performs the same actions as "Make Selected" AND ALSO queues up the actual send schedule in Eloqua @see `Put Live` underbelly, particularly `email_functions.php::scheduleDeployment()`.
  1. Add Eloqua custom subject line to emails.
  1. From Martyn: "I've gone ahead and added the functionality to version CI.2 so that campaign strings are appended to the links in the emails. I added a "campaign" attribute to each of the alerts in the four config files and modified a controller and couple o' views to spit everything out." -- need to port this v3.
1. "Emails Newsletters and Feeds" - missing Newsletter functionality; build this to completion
  1. @see `config/[env]/*feed.php` (Feeds)
  1. @see `config/[env]/pm_config.php`

1. `status: IN PROGRESS` Martyn to revamp email template, Socha to integrate to v2.1.3 @see pm/views/templates/alerts, pm/views/templates/header and determine how/where Martyn needs to implement, determine what customization of php vars is available to him i.e. $body $header.
1. "Fragment Generator" half-baked in v3; make this work. The fragments are output to file, staff copies to Highwire. Identify other fragment usages i.e. Newsletter builds.
  1. @see config/[env]/fragments.php
  1. Fix `->as_html()` func; review `article.php` model. @see ~line 120. @see `simple.xsl` view at pm/view/temlates/sfragments/sci_twis.php
  1. Once fragments are written to network drive, look at weird `section` tag and also debug `img` output; something is choking there.
  1. Generator should be able to target any channel
	Science (sci) Signaling (sig) Science Translational Medicine (stm) -- all come from XML
	News (news) -- comes from RSS feed; presently no configurable data source @see `alert_news_config.php` in v2.1.3 `news_alert_config.php` in v2.1.2
	Migrate conf above to master `pm_config.php`

1. "Ad Content Manager" and "Editorial Content Manager" - do a code review and make sure that functionality is not duplicated; if it is, merge to 1 code base. Review inline TODO notes and resolve as needed @see `application/pm/controllers/alertMaster.php`
