# Tech Spec: <StrategyName>

> Living document. The agent fills it in during intake and UPDATES it throughout development — it is
> the source of truth for requirements. When the logic changes, update the spec first, then the code.

| Field | Value |
|------|----------|
| Strategy | `<StrategyName>` |
| Type | CLIENT / PRODUCT |
| Client | `<ClientName>` (for CLIENT) |
| Template | StrategyTemplate / ControlPanelTemplate / DashboardTemplate |
| Status | draft / in&nbsp;progress / delivered |
| Created / updated | YYYY-MM-DD |

## 1. Goal and brief description
_What the strategy does, the trading idea in 2–3 sentences._

## 2. Market and data
- Instrument(s): 
- Timeframe / bar type: 
- Sessions / trading hours: 
- Additional data series (multi-dataseries): 

## 3. Entry
- Signals / pattern (tree, AND/OR logic): 
- Indicators and their settings: 
- Long conditions: 
- Short conditions: 
- Entry order type (Market/Limit), entry price: 

## 4. Exit / position management
- Stop Loss (ticks / by bar / by signal / custom): 
- Profit Target: 
- Break Even: 
- Trailing: 
- Additional exit conditions (CheckExit): 

## 5. Filters
- Bar filters (volume/delta/%): 
- Time/session filters: 
- Other: 

## 6. Risk management
- Daily loss/profit limit / number of trades: 
- Quantity, number of entries per direction: 

## 7. UI and visualization
- Control Panel (buttons/properties): 
- Pattern Dashboard: 
- Custom plots / drawing: 

## 8. Data export
- Whether needed (IndicatorExport / DrawingObjectsExport), which fields: 

## 9. Open questions
- _What needs clarification from the client/product owner._

## 10. Changelog
- YYYY-MM-DD — spec created (intake).
