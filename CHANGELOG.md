# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 26-09-2025

- Added `MatchMode` enum to support exact matching in `TextMatcher`, with the default being `MatchMode.Fuzzy` for fuzzy searching.

## [0.2.1] - 26-09-2025

- Fixed a bug where using ignore-case comparisons would return match values in uppercase.

## [0.2.0] - 26-09-2025

- Added optional `StringComparison` support for `TextMatcher`, with the default being `StringComparison.CurrentCulture` if not specified.
