# PRSync

- (Modified) Git based projects synchronization utility.
- Useful for PC + Laptop configurations for work synchronization.

## Why git?
Most of my projects use git as a version control system with specific .gitignore files, which should be respected when syncrhonizing between devices to avoid synchronizing temporary files.
Git automatically manages merging when no conflicts are present, which is an added bonus.

## Libgit2

- This project uses a modified version of the libgit2 library, available at https://github.com/MinikPLayer/libgit2-PRSync.
- Modification changes default .git directory name to .prsync to avoid conflicts with existing repositories.
