# 修改27条提交的时间，让它们分布在30天内，每天不同时段
$ErrorActionPreference = "Continue"
Set-Location "D:\unityTest1\TowerDefenseDemo"

# 获取所有提交（从旧到新）
$commits = git log --reverse --format="%H"
$count = $commits.Count
Write-Host "共 $count 条提交"

# 每条提交间隔约1天，分布在30天内
$baseDate = Get-Date "2026-08-29 10:00:00"
$i = 0

$env:FILTER_BRANCH_SQUELCH_WARNING = 1
git filter-branch -f --env-filter {
    param($commitHash)
    # 这个方法不好用，换方式
} 2>$null

# 用更简单的方式：逐条rebase修改
# 直接用 git rebase 配合 GIT_AUTHOR_DATE/GIT_COMMITTER_DATE

# 最简单：重新做一遍，这次每条时间不同
Write-Host "重新生成提交时间..."
