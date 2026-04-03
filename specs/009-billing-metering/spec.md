# Epic 9: Billing & Metering Module

## Overview
The Billing & Metering Module tracks tenant usage across multiple dimensions (concurrent rooms, storage, OTP sends, recording minutes, AI usage) and enforces plan limits. It integrates with HyperPay and Moyasar for GCC-compliant payment processing, manages subscription lifecycle, generates invoices based on actual usage, and provides tenant admins with visibility into billing and plan compliance.

## User Stories

### US-9.1: Tenant Subscription Plan Assignment
**Priority:** P0 (Critical)
**Story Points:** 5

As a platform admin, I want to assign subscription plans to tenants and manage subscription state so that each tenant has appropriate resource limits and billing.

**Acceptance Scenarios:**

**Scenario 1: Create Tenant with Initial Plan**
```gherkin
Given I am a platform admin
When I create a new tenant
  And assign a subscription plan (e.g., "Growth" tier)
Then a Subscription record is created with plan_id, tenant_id, start_date
  And subscription_status = "Active"
  And subscription_start_date = today
  And the tenant can immediately use resources up to plan limits
  And a subscription_created audit event is recorded
```

**Scenario 2: Plan Upgrade Mid-Cycle**
```gherkin
Given a tenant has an active "Basic" subscription
When the tenant admin requests an upgrade to "Growth" plan
  And the upgrade is approved
Then a new Subscription is created with plan_id=Growth, start_date=today
  And the previous "Basic" subscription transitions to status "Superseded"
  And usage meters reset to zero for the new plan
  And billing is prorated: refund for unused Basic time, charge for Growth upgrade
  And a subscription_upgraded audit event is recorded with pricing details
```

**Scenario 3: Plan Downgrade with Enforcement**
```gherkin
Given a tenant on "Enterprise" plan (limit: 100 concurrent rooms)
  And currently running 50 concurrent rooms
When the tenant downgrades to "Growth" plan (limit: 10 concurrent rooms)
Then a new Subscription is created with plan_id=Growth, start_date=today
  And the downgrade is allowed because current usage (50) is checked at downgrade time
  And future room creations enforce the 10-room limit
  And a subscription_downgraded audit event is recorded
```

**Scenario 4: Subscription Suspension**
```gherkin
Given a tenant has an active subscription
When a payment fails and remains unpaid for 7 days
  And suspension is triggered by the billing system
Then the subscription transitions to status "Suspended"
  And all room creation is blocked (no new rooms)
  And existing rooms continue until participant count = 0 (no new joins)
  And a subscription_suspended audit event is recorded with reason
```

**Scenario 5: Reactivate Suspended Subscription**
```gherkin
Given a tenant subscription is "Suspended"
When the overdue payment is received
  And reactivation is triggered
Then the subscription transitions to status "Active"
  And room creation is re-enabled immediately
  And a subscription_reactivated audit event is recorded
```

### US-9.2: Concurrent Room Metering
**Priority:** P0 (Critical)
**Story Points:** 8

As the billing system, I want to track the number of simultaneously live rooms per tenant so that I can enforce concurrent room limits and meter usage.

**Acceptance Scenarios:**

**Scenario 1: Track Concurrent Rooms**
```gherkin
Given a tenant has a subscription with max_concurrent_rooms = 5
When a room transitions to status "Active" (at least 1 participant joined)
  And then a 2nd room transitions to "Active"
  And then a 3rd room transitions to "Active"
Then the concurrent_rooms metric is incremented to 3
  And room_usage_snapshot is updated in cache (Redis) with tenant_id + timestamp
  And no meter snapshot is persisted yet (only at billing cycle end)
```

**Scenario 2: Concurrent Rooms Limit Enforcement**
```gherkin
Given a tenant currently has 5 live rooms (at max limit)
When a user attempts to create a 6th room
Then the room creation is rejected with error "Concurrent room limit exceeded (5/5)"
  And a soft_limit_exceeded audit event is recorded
  And the user is informed of the plan upgrade option
```

**Scenario 3: Room Transitions to Inactive**
```gherkin
Given a tenant with 3 active rooms
When room A transitions to "Inactive" (no participants)
Then the concurrent_rooms metric is decremented to 2
  And the room remains in "Active" status for administrative purposes but does not count toward limit
```

**Scenario 4: Peak Concurrent Rooms Snapshot**
```gherkin
Given a tenant throughout a billing month
When daily metering runs at end of month
Then peak_concurrent_rooms = max(concurrent_rooms across all snapshots)
  And average_concurrent_rooms = avg(concurrent_rooms across all snapshots)
  And both values are stored in BillingMeterSnapshot
```

### US-9.3: Usage Meters and Plan Limits
**Priority:** P0 (Critical)
**Story Points:** 8

As the platform, I want to meter multiple usage dimensions (OTP sends, storage, AI usage, recording minutes) and enforce plan limits.

**Acceptance Scenarios:**

**Scenario 1: OTP Sends Meter**
```gherkin
Given a tenant with plan limit: max_otp_sends = 1000 per month
When OTP is sent for user authentication
  And OTP count increments to 500
  And then OTP count increments to 1000
Then usage_meter[otp_sends] = 1000
  And no warning or blocking occurs (soft limit at 80%, hard limit at 100%)
```

**Scenario 2: OTP Sends Hard Limit Enforcement**
```gherkin
Given a tenant at max_otp_sends = 1000 (100% of limit)
When another OTP send is requested
Then the OTP send is blocked
  And error "OTP limit exceeded for this month"
  And an audit event "hard_limit_exceeded" is recorded
  And the user is informed of the plan upgrade option
```

**Scenario 3: Storage Consumption Meter**
```gherkin
Given files stored in tenant's MinIO bucket
When a storage meter runs daily
Then aggregate file sizes from all FileArtifacts (not deleted, not expired)
  And storage_meter[consumption_bytes] is updated
  And if consumption > plan limit: warning sent to tenant admin
```

**Scenario 4: Recording Minutes Meter**
```gherkin
Given recordings stored for tenant
When each room recording ends
Then recording_minutes is incremented by (end_time - start_time in minutes)
  And if cumulative recording_minutes > plan limit: warning and then blocking
```

**Scenario 5: AI Usage Meter**
```gherkin
Given a tenant with plan limit: max_ai_requests = 5000 per month (reserved capacity, prepaid)
When AI features are used in rooms
Then ai_usage_meter is incremented
  And if usage exceeds reserved capacity: overage charges apply
  And overage rate is defined in plan configuration
```

**Scenario 6: Meter Reset on Billing Cycle**
```gherkin
Given a billing cycle ends on 2026-04-30
  And tenant has used 800/1000 OTP sends
When the cycle rolls to 2026-05-01
Then all usage meters reset to 0
  And new month's allowances are available
  And a meter_reset audit event is recorded with previous month's usage
```

### US-9.4: HyperPay/Moyasar Payment Integration
**Priority:** P0 (Critical)
**Story Points:** 13

As a tenant admin in Saudi Arabia, I want to pay my Muntada subscription using local payment methods (Mada, STC Pay, Apple Pay) via HyperPay or Moyasar.

**Acceptance Scenarios:**

**Scenario 1: Initiate Payment for Invoice**
```gherkin
Given an unpaid invoice is generated
  And tenant admin is viewing the billing dashboard
When I click "Pay Now"
Then the system initiates a payment session with HyperPay
  And generates a unique payment_id and merchant_reference_id
  And redirects tenant to HyperPay hosted payment page
  And payment_page_url is returned with 10-minute expiry
```

**Scenario 2: Payment Success - Mada Card**
```gherkin
Given a payment session is initiated on HyperPay
When tenant completes payment with Mada card
  And HyperPay returns success status
Then a PaymentTransaction record is created with status = "Completed"
  And transaction_id = HyperPay transaction ID
  And payment_method = "mada_card"
  And payment_amount = invoice amount
  And the invoice is marked as paid
  And the tenant's subscription status transitions to "Active" (if was Suspended)
  And a payment_success webhook is sent to the platform
  And an audit event "payment_received" is recorded
```

**Scenario 3: Payment Failure - Declined Card**
```gherkin
Given a payment session is initiated
When the cardholder's card is declined by issuer
  And HyperPay returns failure status with decline_code
Then a PaymentTransaction record is created with status = "Failed"
  And failure_reason = "Card declined"
  And the invoice remains unpaid
  And tenant is redirected to payment failed page
  And an email notification is sent to tenant admin
  And an audit event "payment_failed" is recorded
```

**Scenario 4: Payment Webhook - Async Confirmation**
```gherkin
Given a payment is initiated and tenant closes the browser before completion
When the payment actually completes in HyperPay
Then HyperPay sends a webhook to platform's webhook endpoint
  And the webhook payload includes transaction_id, status, amount, merchant_reference_id
  And the platform validates webhook signature (HMAC-SHA256)
  And PaymentTransaction is updated with status = "Completed" (if not already)
  And the invoice is marked as paid asynchronously
  And a reconciliation audit event is recorded
```

**Scenario 5: STC Pay Payment**
```gherkin
Given a payment session is initiated
When tenant selects "STC Pay" as payment method
  And completes the payment via STC Pay
Then the payment is processed through Moyasar's STC Pay integration
  And PaymentTransaction status transitions to "Completed"
  And payment_method = "stc_pay"
  And all downstream effects (invoice paid, subscription active) apply
```

**Scenario 6: Refund for Plan Downgrade**
```gherkin
Given a tenant was charged 1000 SAR for monthly subscription on 2026-04-01
When tenant downgrades plan on 2026-04-15 (mid-cycle)
Then a refund of 500 SAR (proportional to unused days) is calculated
  And a RefundTransaction is created with status = "Initiated"
  And Moyasar/HyperPay refund API is called with transaction_id and refund_amount
  And RefundTransaction status transitions to "Completed" when confirmed
  And an audit event "refund_issued" is recorded
```

### US-9.5: Invoice Generation
**Priority:** P1 (High)
**Story Points:** 5

As a tenant admin, I want to receive itemized invoices so that I can track my spending and reconcile with my finance team.

**Acceptance Scenarios:**

**Scenario 1: Generate Invoice at Billing Cycle End**
```gherkin
Given a billing cycle ends on 2026-04-30 for a tenant on "Growth" plan at 500 SAR/month
When the invoice generation job runs
Then an Invoice record is created with invoice_number (e.g., "INV-2026-04-001")
  And invoice_date = 2026-05-01
  And billing_period = "2026-04-01 to 2026-04-30"
  And base_plan_charge = 500 SAR (recurring)
  And usage_charges are calculated: recording_minutes_overage, storage_overage, ai_usage_overage
  And total_amount = base_plan_charge + sum(usage_charges)
  And invoice_status = "Pending"
  And a PDF invoice is generated and stored
  And email with invoice PDF is sent to tenant admin
```

**Scenario 2: Invoice with Multiple Line Items**
```gherkin
Given an invoice for a tenant
When I view the invoice details
Then the invoice includes:
  - Line item: "Growth Plan - April 2026" | 500 SAR
  - Line item: "Recording Minutes Overage (100 minutes @ 5 SAR/min)" | 500 SAR
  - Line item: "Storage Overage (50 GB @ 2 SAR/GB)" | 100 SAR
  - Line item: "Tax (15% VAT)" | 165 SAR
  - Total: 1265 SAR
```

**Scenario 3: Invoice Not Generated for Zero Usage**
```gherkin
Given a tenant with no rooms, no recordings, no file transfers
When the billing cycle ends
Then an invoice is still generated (subscription charge applies)
  And invoice shows plan charge but no usage charges
```

**Scenario 4: Invoice Marked as Paid**
```gherkin
Given an invoice with status = "Pending"
When a payment is received and matched to the invoice
Then the invoice status transitions to "Paid"
  And paid_date = payment_date
  And paid_amount = payment amount
  And remaining_balance = 0
```

### US-9.6: Billing Dashboard
**Priority:** P1 (High)
**Story Points:** 5

As a tenant admin, I want to view my billing information, usage meters, and payment history on a dashboard.

**Acceptance Scenarios:**

**Scenario 1: Billing Dashboard Overview**
```gherkin
Given I am a tenant admin
When I navigate to the billing dashboard
Then I see:
  - Current subscription plan name and tier
  - Subscription status (Active, Suspended, Cancelled)
  - Renewal date for next billing cycle
  - Current usage meters (OTP: 450/1000, Storage: 45/100 GB, etc.)
  - Usage as percentage of limits (visual bars)
  - Warnings if > 80% of any limit
```

**Scenario 2: Invoice History**
```gherkin
Given I am viewing the billing dashboard
When I click on "Invoice History"
Then I see a paginated list of all invoices:
  - Invoice number, date, amount, status (Paid/Pending)
  - Download links for PDF invoices
  - Ability to filter by date range or status
  - Ability to export invoice list as CSV
```

**Scenario 3: Payment Methods**
```gherkin
Given I am viewing the billing dashboard
When I click on "Payment Methods"
Then I see:
  - Saved payment methods (Mada card, STC Pay account)
  - Ability to add new payment method
  - Default payment method indicator
  - Ability to remove saved payment methods
```

**Scenario 4: Plan Change**
```gherkin
Given I am viewing the billing dashboard
When I click on "Change Plan"
Then a modal opens showing:
  - Current plan and next 2 available tiers
  - Comparison of limits (concurrent rooms, storage, etc.)
  - Price difference for upgrade or refund for downgrade
  - Confirm/Cancel buttons
```

## Functional Requirements

### Subscription Management
1. **Subscription State Machine**: Subscriptions have states: Draft → Active → Suspended → Cancelled. Only Active subscriptions allow room creation. Only Active/Suspended allow existing rooms to function.
2. **Plan Limit Enforcement**: Every plan has max_concurrent_rooms, max_otp_sends_per_month, max_storage_gb, max_recording_minutes_per_month, max_ai_requests_per_month. Enforce at creation time and reject operations that exceed limits.
3. **Plan Configuration Storage**: Plans are configured in database with: plan_id, plan_name, tier_level, monthly_price_sar, currency, limits (JSON), features (JSON). Plans are immutable once released; new versions create new plan records.
4. **Subscription Lifecycle Dates**: Track subscription_start_date, subscription_renewal_date, subscription_cancelled_date, cancellation_reason.
5. **Billing Cycle Alignment**: All tenants have billing cycles aligned to calendar month (1st to end of month). No mid-cycle plan changes affect billing cycle boundary.

### Usage Metering
6. **Multi-Dimensional Meters**: Maintain separate usage meters for: otp_sends (per month), storage_consumption_bytes (daily), recording_minutes (per month), ai_requests (per month, with reserved capacity), concurrent_rooms (peak + average).
7. **Real-Time Meter Updates**: Meters updated in real-time via event handlers. When room starts recording: increment recording meter. When file uploaded: increment storage meter. Meters persist to database at end of day (daily snapshot).
8. **Soft Limit Warnings**: When usage reaches 80% of plan limit, generate warning notification and audit event. User is informed but operation is not blocked.
9. **Hard Limit Enforcement**: When usage reaches 100% of plan limit, block further operations. Return error with plan upgrade suggestion.
10. **Meter Snapshots**: End-of-month job creates BillingMeterSnapshot for each tenant with: snapshot_date, tenant_id, subscription_id, otp_sends_used, storage_bytes_peak, recording_minutes_used, ai_requests_used, concurrent_rooms_peak, concurrent_rooms_avg.
11. **Overage Pricing**: Usage beyond plan limits is charged at defined overage rates: recording_minutes_overage_rate_sar, storage_overage_rate_sar_per_gb, ai_overage_rate_sar_per_request. Rates defined in plan configuration.

### Payment Integration
12. **HyperPay API Integration**: Implement REST client for HyperPay payment initiation. POST /payment/init with invoice_id, amount, currency, merchant_reference_id, callback_url. Returns payment_url (valid for 10 minutes).
13. **Moyasar API Integration**: Fallback or alternative payment processor. Support Moyasar's API for payment processing. Configuration allows switching between HyperPay and Moyasar via environment variable.
14. **Webhook Signature Validation**: All webhooks from HyperPay/Moyasar are validated with HMAC-SHA256 signature using shared secret. Invalid signatures are rejected and logged.
15. **Webhook Idempotency**: Webhook handlers check PaymentTransaction.hyperpay_transaction_id before updating state. If already processed, webhook is acknowledged but no state change.
16. **Payment Timeout Handling**: If payment status is not confirmed within 1 hour of initiation, a timeout event is recorded and tenant is notified.

### Invoice Management
17. **Invoice Generation Job**: End-of-month batch job (runs on 1st of month, 00:00 UTC) generates invoices for all tenants with active subscriptions. Invoices are immutable once generated (no editing).
18. **Invoice Numbering**: Invoices numbered sequentially per tenant per year: INV-{YYYY}-{TENANT_ID}-{SEQUENCE}. Sequence resets yearly.
19. **Invoice Line Items**: Base charge (subscription), usage overages, prorations, taxes, discounts. Each line item includes description, unit rate, quantity, amount.
20. **Tax Calculation**: VAT 15% applied to total_before_tax. Stored separately in invoice record.
21. **Invoice Storage**: PDF generated via templating engine. Stored in MinIO under tenant bucket. URL persisted in Invoice record for download access.

### Key Entities

**Subscription**
- `subscription_id` (UUID, PK)
- `tenant_id` (UUID, FK → Tenant, unique per active subscription)
- `plan_id` (UUID, FK → Plan)
- `status` (enum: Draft, Active, Suspended, Cancelled, default: Draft)
- `subscription_start_date` (datetime)
- `subscription_renewal_date` (datetime)
- `subscription_cancelled_date` (datetime, nullable)
- `cancellation_reason` (string, nullable)
- `trial_end_date` (datetime, nullable)
- `auto_renew` (bool, default: true)
- `created_at_utc` (datetime)
- `updated_at_utc` (datetime)

**BillingMeterSnapshot**
- `snapshot_id` (UUID, PK)
- `tenant_id` (UUID, FK → Tenant)
- `subscription_id` (UUID, FK → Subscription)
- `billing_period_start` (datetime)
- `billing_period_end` (datetime)
- `otp_sends_used` (int, default: 0)
- `storage_bytes_peak` (long, default: 0)
- `storage_bytes_avg` (long, default: 0)
- `recording_minutes_used` (int, default: 0)
- `ai_requests_used` (int, default: 0)
- `concurrent_rooms_peak` (int, default: 0)
- `concurrent_rooms_avg` (decimal, default: 0)
- `created_at_utc` (datetime)

**UsageMeter (Current Month)**
- `meter_id` (UUID, PK)
- `tenant_id` (UUID, FK → Tenant, unique per month)
- `billing_month` (datetime, YYYYMM format)
- `otp_sends_used` (int, default: 0)
- `storage_bytes_current` (long, default: 0)
- `recording_minutes_used` (int, default: 0)
- `ai_requests_used` (int, default: 0)
- `concurrent_rooms_current` (int, default: 0)
- `updated_at_utc` (datetime)

**Invoice**
- `invoice_id` (UUID, PK)
- `invoice_number` (string, unique per tenant per year)
- `tenant_id` (UUID, FK → Tenant)
- `subscription_id` (UUID, FK → Subscription)
- `invoice_date` (datetime)
- `billing_period_start` (datetime)
- `billing_period_end` (datetime)
- `status` (enum: Pending, Paid, Overdue, Cancelled)
- `subtotal_sar` (decimal, 2 places)
- `tax_amount_sar` (decimal, 2 places)
- `total_amount_sar` (decimal, 2 places)
- `paid_amount_sar` (decimal, 2 places, default: 0)
- `remaining_balance_sar` (decimal, 2 places)
- `paid_date` (datetime, nullable)
- `due_date` (datetime)
- `minio_pdf_url` (string, nullable)
- `created_at_utc` (datetime)
- `updated_at_utc` (datetime)

**PaymentTransaction**
- `transaction_id` (UUID, PK)
- `invoice_id` (UUID, FK → Invoice, nullable)
- `tenant_id` (UUID, FK → Tenant)
- `payment_method` (enum: mada_card, stc_pay, apple_pay, credit_card)
- `amount_sar` (decimal, 2 places)
- `status` (enum: Initiated, Completed, Failed, Refunded)
- `hyperpay_transaction_id` (string, nullable, unique)
- `hyperpay_merchant_reference_id` (string, nullable)
- `moyasar_transaction_id` (string, nullable)
- `failure_reason` (string, nullable)
- `payment_timestamp` (datetime, nullable)
- `webhook_received_timestamp` (datetime, nullable)
- `currency` (string, default: "SAR")
- `created_at_utc` (datetime)
- `updated_at_utc` (datetime)

**RefundTransaction**
- `refund_id` (UUID, PK)
- `original_transaction_id` (UUID, FK → PaymentTransaction)
- `invoice_id` (UUID, FK → Invoice)
- `tenant_id` (UUID, FK → Tenant)
- `refund_reason` (enum: plan_downgrade, overpayment, cancellation)
- `refund_amount_sar` (decimal, 2 places)
- `status` (enum: Initiated, Completed, Failed)
- `hyperpay_refund_id` (string, nullable)
- `refund_timestamp` (datetime, nullable)
- `created_at_utc` (datetime)
- `updated_at_utc` (datetime)

## Success Criteria

- [ ] Subscription status changes are enforced immediately (Active/Suspended blocks room creation within 1 second)
- [ ] Usage meters are updated within 500ms of usage event (OTP send, file upload, room start, etc.)
- [ ] Hard limits prevent operations 100% of the time when exceeded
- [ ] Payment initiation with HyperPay completes in < 2 seconds
- [ ] Webhook signatures validated correctly for 100% of incoming webhooks
- [ ] Invoices generated within 1 hour of billing cycle end for all tenants
- [ ] Invoice PDF generation completes in < 5 seconds per invoice
- [ ] Billing dashboard loads with all metrics in < 3 seconds
- [ ] Soft limit warnings sent to tenant admin when usage reaches 80% of limit
- [ ] Payment reconciliation matches HyperPay/Moyasar records within 24 hours with 100% accuracy
- [ ] Refund processing via HyperPay/Moyasar completes within 48 hours
- [ ] All billing-related actions produce immutable audit events
- [ ] Meter snapshots are accurate to within 1% for all usage dimensions

## Edge Cases

1. **Plan Change During Billing Cycle**: Tenant upgrades plan on day 15 of 30-day month. Prorating logic calculates: (old_plan_remaining_charge * 15/30) refunded, new plan charged for remaining 15 days. Both charges reflected in same invoice.

2. **Payment Failure Followed by Successful Retry**: Payment fails due to network timeout. Tenant retries with same card. System detects previous Failed transaction via hyperpay_transaction_id. New PaymentTransaction created for retry. Both transactions are logged.

3. **Concurrent Rooms Spike Mid-Month**: Tenant creates 8 rooms on day 20 (when limit is 5). Operation is blocked. Tenant calls support. Admin manually increases plan limit to 10 rooms retroactively. Tenant can now create rooms. Invoice adjusted with credit for over-charged soft limit warnings.

4. **Subscription Suspension During Active Room**: Tenant subscription suspended due to payment failure. Room has 3 participants. Room continues until last participant leaves. New participants cannot join. Event: room_suspension_enforced is audited.

5. **Meter Snapshot Created at Month Boundary**: Midnight UTC on 2026-04-30 triggers invoice generation. Concurrent room metric at 23:59:59 UTC is 5. At 00:00:00 UTC, metric resets. Invoice reflects peak of 5 rooms for April.

6. **Overage Pricing for Multiple Dimensions**: Tenant exceeds both recording minutes and storage in same month. Invoice includes: base charge, recording_overage_line_item, storage_overage_line_item, tax on total.

7. **Refund Processing Fails**: Refund initiated via HyperPay API. HyperPay returns error (network issue). RefundTransaction status = Failed. Retry scheduled for 1 hour later. Admin is notified via audit event.

8. **Invoice Generation Fails Midway**: Job crashes after generating 50 of 100 tenant invoices. Job resumes and detects already-processed tenants via invoice_date uniqueness constraint. Continues with remaining 50.

9. **Payment Webhook Arrives Before Status Poll**: Payment initiated at T+0. Tenant navigates away. Webhook arrives at T+5s and updates PaymentTransaction to Completed. At T+1m, client polls for status and gets immediate confirmation.

10. **Currency Mismatch**: Admin creates invoice in USD by mistake. Invoice record has currency = USD. Payment processor expects SAR. Payment is rejected. Manual correction required.

## Assumptions

1. **HyperPay/Moyasar Always Available**: Assume payment processors have 99.9% uptime SLA. Fallback to manual payment processing not in scope.

2. **Invoice Generation Is Scheduled**: Assume background job runs reliably at 00:00 UTC on 1st of month. No manual invoice generation needed.

3. **Usage Meters Are Synchronized**: Assume all services report usage events consistently. No reconciliation needed between meter sources.

4. **Tax Rate is 15% VAT**: Assume all invoices subject to 15% VAT per Saudi PDPL. No locale-specific tax variations in v1.

5. **Plan Limits Are Plan-Immutable**: Once a plan is released, its limits cannot change. New plan versions create new plan records.

6. **Subscription Renewal is Automatic**: Assume auto_renew = true by default. Tenants must explicitly cancel to prevent renewal.

7. **Billing Cycle Aligns to Calendar Month**: No custom billing cycles (e.g., every 30 days from signup date). All tenants use Jan 1 - Dec 31 calendar year for planning.

8. **Payment Method Storage is PCI-Compliant**: Assume payment processor (HyperPay/Moyasar) handles PCI compliance. Platform does not store full card details.

9. **Webhook Retries Handled by Processor**: Assume HyperPay/Moyasar retries webhooks on platform timeout. Platform does not retry incoming webhooks.

10. **Concurrent Room Definition**: A room is counted as "concurrent" only if status = "Active" AND has at least 1 participant. Empty active rooms do not count.

## Dependencies

- **Epic 2 (Tenancy)**: Subscriptions and billing configuration tied to Tenant. Tenant admin role required for billing dashboard access.
- **Epic 3 (Rooms)**: Usage metering for concurrent rooms requires Room entity status and participant count. Recording metering requires recording metadata.
- **Epic 8 (Files & Artifacts)**: Storage metering requires file size aggregation from FileArtifact records.
- **HyperPay API**: Payment processing integration. Must be deployed and accessible. API documentation: https://hyperpay.io/docs (assumed).
- **Moyasar API**: Alternative or fallback payment processor. Must be deployed and accessible.
- **Background Job Scheduler**: Invoice generation job requires reliable scheduling (e.g., Quartz, Hangfire, or custom).
- **RabbitMQ**: Optional for asynchronous payment webhook handling and meter aggregation jobs.
- **MinIO**: Invoice PDF storage and retrieval.
- **Redis**: Optional caching for meter snapshots to reduce database load during high-frequency updates.

---

**Document Version:** 1.0
**Last Updated:** 2026-04-03
**Module Owner:** Finance Engineering Team
**Status:** Ready for Implementation
