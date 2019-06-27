#!/usr/bin/env python

import sys
import os
import re

from functools import partial
from datetime import date

try:
    from github import Github
except ImportError:
    sys.stderr.write("Install 'pip install PyGithub'")
    sys.exit(1)


me = os.path.dirname(__file__)

source_pattern = re.compile(r'\s*public const string Version = \"(?P<v>[\d\.]+)\";', re.MULTILINE)
source_extension_cs = os.path.join(me, "Conan.VisualStudio", "source.extension.cs")

vsixmanifest_pattern = re.compile(r'\s+<Identity .*Version=\"(?P<v>[\d\.]+)\".*', re.MULTILINE)
source_vsixmanifest_cs = os.path.join(me, "Conan.VisualStudio", "source.extension.vsixmanifest")


def get_current_version():
    v_source_extension = None
    v_source_manifest = None

    # Get version from 'source.extension.cs'
    for line in open(source_extension_cs, "r").readlines():
        m = source_pattern.match(line)
        if m:
            v_source_extension = m.group("v")
    # Get version from 'source.extension.vsixmanifest'
    for line in open(source_vsixmanifest_cs, "r").readlines():
        m = vsixmanifest_pattern.match(line)
        if m:
            v_source_manifest = m.group("v")

    assert v_source_extension == v_source_manifest, "Versions in {!r} and {!r} are different:" \
            " {!r} != {!r}".format(source_extension_cs, source_vsixmanifest_cs, v_source_extension, v_source_manifest)
    return v_source_extension


def set_current_version(version):
    v_source_extension = None
    v_source_manifest = None

    def replace_closure(subgroup, replacement, m):
        if m.group(subgroup) not in [None, '']:
            start = m.start(subgroup)
            end = m.end(subgroup)
            return m.group(0)[:start] + replacement + m.group(0)[end:]

    # Substitute version in 'source.extension.cs'
    lines = []
    for line in open(source_extension_cs, "r").readlines():
        line_sub = source_pattern.sub(partial(replace_closure, "v", version), line)
        lines.append(line_sub)
    with open(source_extension_cs, "w") as f:
        f.write("".join(lines))

    # Substitute version in 'source.extension.vsixmanifest'
    lines = []
    for line in open(source_vsixmanifest_cs, "r").readlines():
        line_sub = vsixmanifest_pattern.sub(partial(replace_closure, "v", version), line)
        lines.append(line_sub)
    with open(source_vsixmanifest_cs, "w") as f:
        f.write("".join(lines))


def write_changelog(version, prs):
    print("*"*20)
    changelog = os.path.join(me, "CHANGELOG.md")

    version_content = ["- {} ([#{}]({}))\n".format(pr.title, pr.number, pr.html_url) for pr in prs]
    sys.stdout.write("*"*20)
    sys.stdout.write("\n{}".format(''.join(version_content)))
    sys.stdout.write("*"*20)
    sys.stdout.write("\n\n")
    if not query_yes_no("This is the list of items that will be added to the CHANGELOG"):
        sys.stdout.write("Exit!")
        sys.exit(1)

    new_content = []
    changelog_found = False
    version_pattern = re.compile("## [\d\.]+")
    for line in open(changelog, "r").readlines():
        if not changelog_found:
            changelog_found = bool(line.strip() == "# Changelog")
        else:
            if version_pattern.match(line):
                # Add before new content
                new_content.append("## {}\n\n".format(version))
                new_content.append("**{}**\n\n".format(date.today().strftime('%Y-%m-%d')))
                new_content += version_content
                new_content.append("\n\n")
        new_content.append(line)

    with open(changelog, "w") as f:
        f.write("".join(new_content))


def get_git_current_branch():
    return os.popen('git rev-parse --abbrev-ref HEAD').read().strip()

def get_git_is_clean():
    return len(os.popen('git status --untracked-files=no --porcelain').read().strip()) == 0

def query_yes_no(question, default="yes"):
    valid = {"yes": True, "y": True, "ye": True,
             "no": False, "n": False}
    if default is None:
        prompt = " [y/n] "
    elif default == "yes":
        prompt = " [Y/n] "
    elif default == "no":
        prompt = " [y/N] "
    else:
        raise ValueError("invalid default answer: '%s'" % default)

    while True:
        sys.stdout.write(question + prompt)
        choice = input().lower()
        if default is not None and choice == '':
            return valid[default]
        elif choice in valid:
            return valid[choice]
        else:
            sys.stdout.write("Please respond with 'yes' or 'no' (or 'y' or 'n').\n")

def work_on_release(next_release):
    github_token = os.environ.get("GITHUB_TOKEN")
    if not github_token:
        sys.stderr.write("Please, provide a read-only token to access Github using environment variable 'GITHUB_TOKEN'\n")

    # Find matching milestone
    g = Github(github_token)
    repo = g.get_repo('conan-io/conan-vs-extension')
    open_milestones = repo.get_milestones(state='open')
    for milestone in open_milestones:
        if str(milestone.title) == next_release:
            # Gather pull requests
            prs = [it for it in repo.get_pulls(state="all") if it.milestone == milestone]
            sys.stdout.write("Found {} pull request for this milestone:\n".format(len(prs)))
            for p in prs:
                status = "[!]" if p.state != "closed" else ""
                sys.stdout.write("\t {}\t#{} {}\n".format(status, p.number, p.title))
            
            # Gather issues
            issues = [it for it in repo.get_issues(milestone=milestone, state="all")]
            sys.stdout.write("Found {} issues for this milestone:\n".format(len(issues)))
            for issue in issues:
                status = "[!]" if issue.state != "closed" else ""
                sys.stdout.write("\t {}\t#{} {}\n".format(status, issue.number, issue.title))
            
            # Any open PR or issue?
            if any([p.state != "closed" for p in prs]) or any([issue.state != "closed" for issue in issues]):
                sys.stderr.write("Close all PRs and issues belonging to the milestone before making the release")
                return
            
            # Checkout the release branch and commit the changes
            os.system('git checkout -b release/{}'.format(next_release))

            # Modify the working directory
            set_current_version(next_release)
            prs = [pr for pr in prs if pr.merged]
            write_changelog(next_release, prs)


            if query_yes_no("Commit and push to 'conan' repository"):
                os.system("git add CHANGELOG.md")
                os.system("git add Conan.VisualStudio/source.extension.cs")
                os.system("git add Conan.VisualStudio/source.extension.vsixmanifest")

                os.system('git commit -m "Preparing release {}"'.format(next_release))
                os.system('git push --set-upstream conan release/1.1.0')

                sys.stdout.write("Now create PR to 'master' and PR back to 'dev'")
                pr = repo.create_pull(title="Release {}".format(next_release),
                                      head="release/{}".format(next_release),
                                      base="master",
                                      body="Release {}. Don't forget to create the tag after merging!".format(next_release))

                repo.create_pull(title="Merge back release branch {}".format(next_release),
                                head="release/{}".format(next_release),
                                base="dev",
                                body="Merging back changes from release branch {}. Don't merge before #{}".format(next_release, pr.number))
            else:
                sys.stdout.write("You will need to commit and push yourself, and to create the PRs")
            break
    else:
        sys.stderr.write("No milestone matching version {!r}. Open milestones found were '{}'\n".format(next_release, "', '".join([it.title for it in open_milestones])))


if __name__ == "__main__":
    current_branch = get_git_current_branch()
    if current_branch != "dev":
        sys.stderr.write("Move to the 'dev' branch to work with this tool. You are in '{}'\n".format(current_branch))
        sys.exit(1)
    
    if not get_git_is_clean():
        sys.stderr.write("Current branch is not clean\n")
        sys.exit(1)

    v = get_current_version()
    sys.stdout.write("Current version is {!r}\n".format(v))

    major, minor, _ = v.split(".")
    next_release = ".".join([major, str(int(minor)+1), "0"])
    if query_yes_no("Next version will be {!r}".format(next_release)):
        work_on_release(next_release)
        
    else:
        sys.stdout.write("Sorry, I cannot help you then...")
