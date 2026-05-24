# QA Testing Scenarios
## Positive
- Create ticket with valid payload.
- Resolve ticket as assigned agent.
## Negative
- Unauthorized access returns 401.
- Invalid status transition rejected.
## Boundary
- File upload 5MB accepted, 5MB+ rejected.
- Title length min/max validation.
## Security
- SQLi payload has no effect.
- XSS payload not executed.
## UI/Cross-browser
- Chrome, Edge, Firefox desktop/mobile smoke tests.

