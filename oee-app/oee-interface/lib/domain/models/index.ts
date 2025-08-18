/**
 * Domain Models
 * 
 * Simple, pragmatic domain models that add business meaning
 * without unnecessary complexity.
 */

// OEE Value Objects - The three factors and overall calculation
export { Availability } from './Availability'
export { Performance } from './Performance'
export { Quality } from './Quality'
export { OEE_Calculation } from './OEE_Calculation'

// Business Entities - Canonical model alignment
export { Work_Order } from './Work_Order'
export { Downtime_Record } from './Downtime_Record'