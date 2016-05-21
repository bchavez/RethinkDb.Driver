General Help
------------
General help with ReQL questions can be found on:

* **Slack**: Primary means of getting help is on [**Slack**](http://slack.rethinkdb.com) <a href="http://slack.rethinkdb.com/"><img valign="middle"  src="http://slack.rethinkdb.com/badge.svg"></a>  If you don't receive the invite, check your spam folder.
* **IRC Freenode**: Use [WebChat](http://webchat.freenode.net/?channels=rethinkdb) and join `#rethinkdb` channel. Alternatively, [HexChat](https://hexchat.github.io/) a good native windows IRC client.
  

Pull Requests
------------
Err'body hatin' on legal :-1:, but gotta do it. Please sign the [**RethinkDB CLA**](http://rethinkdb.com/community/cla/). :pencil2: :clipboard: Technically, it isn't legally binding for a community driver, but in the event this driver is adopted as an official driver we want to have all our ducks in a row.

Once the CLA is signed, here are some helpful guidelines to keep in mind when contributing.  While following them isn't absolutely required, it does help everyone to accept your pull-requests with maximum awesomeness.

* :heavy_check_mark: **CONSIDER** adding a unit test if your PR resolves an issue.
* :heavy_check_mark: **DO** keep pull requests small so they can be easily reviewed. 
* :heavy_check_mark: **DO** make sure unit tests pass.
* :heavy_check_mark: **DO** make sure any public APIs are XML documented. [We don't need no stinkin' `CS1591`](https://www.youtube.com/watch?v=nsdZKCh6RsU). =) 
* :heavy_check_mark: **DO** make sure not to introduce any compiler :warning: warnings. We keep it at :zero: 'cuz that's how we roll. :dollar:
* :x: **AVOID** breaking the continuous integration build. 
* :x: **AVOID** making significant changes to the driver's overall architecture. We'd like to keep this driver in-sync with the overall architecture of the Java driver so both projects benefit from bug fixes and new features.

