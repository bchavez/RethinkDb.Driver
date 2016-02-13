General Help
------------
General help with ReQL questions can be found on:

* **Slack**: Primary means of getting help is on [**Slack**](http://slack.rethinkdb.com) <a href="http://slack.rethinkdb.com/"><img valign="middle"  src="http://slack.rethinkdb.com/badge.svg"></a>  If you don't receive the invite, check your spam folder for the invite.
* **IRC Freenode**: Use [WebChat](http://webchat.freenode.net/?channels=rethinkdb) and join `#rethinkdb` channel. There are many online that can help answer general ReQL questions. Alternatively, [HexChat](https://hexchat.github.io/) a good native windows IRC client. 
* **Gitter**: For questions that relate to this C# driver we have a **Gitter** channel: [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/bchavez/RethinkDb.Driver?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
  

Reporting Issues
------------
Here are some helpful tips to keep in mind when reporting issues:

* :heavy_check_mark: **CONSIDER** including [JSON protocol traces](https://github.com/bchavez/RethinkDb.Driver/wiki/Protocol-Debugging) in your issue.
* :heavy_check_mark: **CONSIDER** including any **ReQL** queries (and any associated classes) in your issue.
* :heavy_check_mark: **CONSIDER** describing the OS and run-time platform you're using.

Pull Requests
------------
Err'body hatin' on legal :-1:, but gotta do it. Please sign the [**RethinkDB CLA**](http://rethinkdb.com/community/cla/). :pencil2: :clipboard:

Once the CLA is signed, here are some helpful guidelines to keep in mind when contributing.  While following them isn't absolutely required, it does help everyone to accept your pull-requests with maximum awesomeness.

* :heavy_check_mark: **CONSIDER** adding a unit test if your PR resolves an issue.
* :heavy_check_mark: **DO** keep pull requests small so they can be easily reviewed. 
* :heavy_check_mark: **DO** make sure unit tests pass.
* :x: **AVOID** breaking the continuous integration build. 
* :x: **AVOID** making significant changes to the driver's overall architecture. We'd like to keep this driver in-sync with the overall architecture of the Java driver so both projects benefit from bug fixes and new features. 

