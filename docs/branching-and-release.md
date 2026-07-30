# Branching and release flow

We aim to follow [Gitlab Flow](https://about.gitlab.com/topics/version-control/what-is-gitlab-flow/), a lightweight Gitflow alternative.
What does that mean for you as a contributor or maintainer of this project?

## How to contribute code

1. You develop on a local fork
2. You raise a PR once your work is ready for review
3. Target `main` on the Fallout-Upstream
4. Your code gets merged

## How to publish a new release

Sometimes it becomes necessary to create a stabilisation branch to make sure we iron out the worst bugs before pushing a release.
For this purpose Gitlab Flow allows us to create branches, i.e. `release/v10.4`
> [!Note] While a release branch exists, it becomes necessary to raise some PRs against `release/v10.4` and **then** upmerge those changes against `main` as well

Once we feel comfortable with our release, we can `git tag` our release with the appropiate version, which triggers our CI to run the publish release pipeline.

- TODO: put in the correct references here
- TODO: Mermaid diagram showing the branches and maybe a few examples of how to merge
- TODO: cli commands examples for release candidate and actual release
