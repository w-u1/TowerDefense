#!/bin/bash
# 修改所有提交日期，分布在30天内
cd /d/unityTest1/TowerDefenseDemo

count=$(git rev-list --count HEAD)
echo "共 $count 条提交"

# 从最早的提交开始，每条间隔约1天多
base_ts=$(date -d "2026-08-29 09:00:00" +%s)

# 获取所有commit hash（从旧到新）
commits=($(git log --reverse --format="%H"))

GIT_SEQUENCE_EDITOR="sed -i -e 's/^pick/reword/g'" git rebase -i HEAD~$count 2>&1 | head -5
