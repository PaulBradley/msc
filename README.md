<img src="./docs/icons/go.svg?raw=true"     width="64" height="64" />&nbsp;
<img src="./docs/icons/cs.svg?raw=true"     width="64" height="64" />&nbsp;
<img src="./docs/icons/dotnet.svg?raw=true" width="64" height="64" />&nbsp;
<img src="./docs/icons/aws.svg?raw=true"    width="64" height="64" />

# Medical Spell Checker API

Based on [https://hunspell.github.io/](hunspell), `msc` adds 90,142 medical terms
to an already comprehensive dictionary of English words.

`msc` was originally developed to passively spell check care plans as the data
was pulled into a Discharge Summary Document. The `msc` API offers a free-to-use
spell checker system which will be hosted on AWS as a [Lambda Function URL](https://docs.aws.amazon.com/lambda/latest/dg/urls-invocation.html).

Clinical systems will be able to pass text to the `msc` API and get a list of
misspellings returned as `JSON`, along with a list of suggested replacements.

Users of the API will be able to request new words to be added to the
dictionary by opening [an issue](https://github.com/PaulBradley/msc/issues).

## TODO

* Add dictionaries for surnames and forenames, so that patients and staff names don't get flagged as miss-spellings.
