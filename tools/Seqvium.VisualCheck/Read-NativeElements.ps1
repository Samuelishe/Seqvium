# SPDX-License-Identifier: Apache-2.0
param([Parameter(Mandatory=$true)][long]$WindowHandle)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$WindowHandle)
$elements = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
$result = foreach ($element in $elements) {
    $current = $element.Current
    if (-not $current.IsOffscreen -and $current.BoundingRectangle.Width -gt 0) {
        [pscustomobject]@{ Name=$current.Name; Id=$current.AutomationId; Type=$current.ControlType.ProgrammaticName;
            X=$current.BoundingRectangle.X; Y=$current.BoundingRectangle.Y;
            Width=$current.BoundingRectangle.Width; Height=$current.BoundingRectangle.Height }
    }
}
ConvertTo-Json -InputObject @($result) -Compress -Depth 4
