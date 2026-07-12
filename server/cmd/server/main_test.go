package main

import (
	"testing"
	"time"
)

func TestParseBattleIntervalAllowsFastIntegrationRuns(t *testing.T) {
	if got := parseBattleInterval("25ms"); got != 25*time.Millisecond {
		t.Fatalf("interval = %v, want 25ms", got)
	}
	if got := parseBattleInterval("invalid"); got != defaultBattleInterval {
		t.Fatalf("invalid interval = %v, want default %v", got, defaultBattleInterval)
	}
}
