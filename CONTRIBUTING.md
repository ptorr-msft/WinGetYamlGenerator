# Contributing to this repo

First, thanks for your interest!

Second, this is a very simple project so hopefully won't need a lot of guidance. Just a few things:

## Versioning.

Please update the version in `package.appxmanifest` with each PR. Feel free to update the `minor` version (second number) for 
anything non-trivial, but if it's a very minor change you could update the `build` (third number). 

Also please keep the SDK and min-OS version the same, so the app can be used on the largest set of machines. Using
light-up with `ApiInformation` or XAML namespaces is fine. If you need to bump the version for some reason, please open an
issue first and we can discuss.

## Style, line endings, etc.

Please make sure you follow the style of the code. For example, braces are on separate lines, and always use braces even for
single-line blocks:

```c#
if (foo)
{
  Stuff();
}
```

Please keep spaces instead of tabs (4 spaces) and keep the line endings as CR-LF; this just avoids unnecessary churn.

## Other

Please open an issue if you have any other ideas!
