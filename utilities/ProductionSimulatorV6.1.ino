#include <Arduino.h>

// Constants
const int PRODUCTION_PIN = 13;         // Sensor pin
const int REST_PERIOD_PIN = 12;        // Rest pin
const int SETUP_PIN = 11;              // Setup pin
const unsigned long TIME_UNIT = 60000; // Time unit in milliseconds, currently set to 1 minute

const unsigned long SETUP_DURATION_MEDIAN = 25 * TIME_UNIT; // Setup time median
const unsigned long JOB_SIZE_MIN = 1500;                    // Minimum job size in units
const unsigned long JOB_SIZE_MAX = 15000;                   // Maximum job size in units

const unsigned long LUNCH_BREAK_DURATION = 30 * TIME_UNIT;     // Duration of a lunch break
const unsigned long LUNCH_BREAK_INTERVAL = 4 * 60 * TIME_UNIT; // Interval between lunch breaks

const unsigned long STOPPAGE_DURATION_MEDIAN = 10 * TIME_UNIT;  // Median stoppage duration
const unsigned long MIN_STOPPAGE_INTERVAL = 30 * TIME_UNIT;     // Minimum interval between stoppages
const unsigned long MAX_STOPPAGE_INTERVAL = 4 * 60 * TIME_UNIT; // Maximum interval between stoppages
const unsigned long MAX_UNITS_BEFORE_STOPPAGE = 5000;           // Maximum units before a stoppage
const unsigned long RECENT_STOPPAGE_INTERVAL = 15 * TIME_UNIT;  // Time interval that increases stoppage probability
const float RECENT_STOPPAGE_FACTOR = 0.5;                       // Factor that increases stoppage probability after a recent stoppage

const unsigned long MIN_PRODUCTION_RATE = 5 * TIME_UNIT;  // Minimum production rate
const unsigned long MAX_PRODUCTION_RATE = 20 * TIME_UNIT; // Maximum production rate
const float PRODUCTION_RATE_STDDEV = 0.2;                 // Standard deviation as a fraction of the target production rate

const unsigned long MIN_RAMP_UP_TIME = 15 * TIME_UNIT; // Minimum ramp up time
const unsigned long MAX_RAMP_UP_TIME = 60 * TIME_UNIT; // Maximum ramp up time
const float INITIAL_RAMP_UP_RATE = 0.2;                // Initial production rate during ramp up, as a fraction of the target production rate

// Variables
enum State
{
    SETUP,
    RUNNING,
    STOPPAGE,
    RAMP_UP,
    LUNCH_BREAK
};
State state;
unsigned long setupTime, stoppageTime, rampUpTime, rampUpDuration;
unsigned long unitsInJob, unitsProduced, totalUnitsProduced;
unsigned long lunchBreakTime, nextLunchBreak;
unsigned long stoppageInterval, nextStoppage;
unsigned long productionRate, nextUnitTime;
bool recentStoppage;

// Function to generate a normal distribution
float generateNormalDistribution(float mean, float stdDev)
{
    float u = static_cast<float>(random(1, 32767)) / 32767.0;
    float v = static_cast<float>(random(1, 32767)) / 32767.0;
    float z = sqrt(-2.0 * log(u)) * cos(2.0 * PI * v);
    return mean + z * stdDev;
}

// Function to generate a Setup pulse
void setupPulse()
{
    digitalWrite(SETUP_PIN, HIGH);
    delay(500);
    digitalWrite(SETUP_PIN, LOW);
}

void produceUnit()
{
    if (millis() >= nextUnitTime)
    {
        digitalWrite(PRODUCTION_PIN, HIGH);
        delay(100);
        digitalWrite(PRODUCTION_PIN, LOW);

        unitsProduced++;
        totalUnitsProduced++;

        float prodRate = generateNormalDistribution(productionRate, productionRate * PRODUCTION_RATE_STDDEV);
        nextUnitTime = millis() + prodRate * TIME_UNIT;
    }
}

void stopProduction()
{
    nextUnitTime = millis() + random(MIN_STOPPAGE_INTERVAL, MAX_STOPPAGE_INTERVAL);
}

void resumeProduction()
{
    rampUpTime = random(MIN_RAMP_UP_TIME, MAX_RAMP_UP_TIME);
    nextUnitTime = millis() + rampUpTime;
}

void checkSetupTime()
{
    if (millis() >= setupTime)
    {
        unitsInJob = random(JOB_SIZE_MIN, JOB_SIZE_MAX);
        productionRate = random(MIN_PRODUCTION_RATE, MAX_PRODUCTION_RATE);
        resumeProduction();
    }
}

void checkRampUpComplete()
{
    if (millis() >= nextUnitTime)
    {
        rampUpTime = 0;
    }
}

void checkStoppageTime()
{
    if (unitsProduced >= MAX_UNITS_BEFORE_STOPPAGE)
    {
        stopProduction();
    }
}

void checkRestPeriodTime()
{
    if (millis() - lunchBreakTime >= LUNCH_BREAK_DURATION)
    {
        lunchBreakTime = millis();
        stopProduction();
    }
}

void setup()
{
    // Initialize variables
    state = SETUP;
    unitsProduced = 0;
    totalUnitsProduced = 0;
    lunchBreakTime = 0;
    nextLunchBreak = LUNCH_BREAK_INTERVAL;
    nextStoppage = STOPPAGE_DURATION_MEDIAN;
    productionRate = MIN_PRODUCTION_RATE;
    nextUnitTime = millis() + productionRate;

    // Initialize the sensor pin
    pinMode(PRODUCTION_PIN, OUTPUT);
    pinMode(REST_PERIOD_PIN, OUTPUT);
    pinMode(SETUP_PIN, OUTPUT);
}

void loop()
{
    switch (state)
    {
    case SETUP:
        setupPulse(); // Generate a setup pulse
        checkSetupTime();
        break;

    case RUNNING:
        produceUnit();
        checkRestPeriodTime();
        checkStoppageTime();
        break;

    case STOPPAGE:
        stopProduction();
        break;

    case RAMP_UP:
        resumeProduction();
        checkRampUpComplete();
        break;

    case LUNCH_BREAK:
        stopProduction();
        checkRestPeriodTime();
        break;
    }

    // State transitions
    if (state == SETUP && millis() >= setupTime)
    {
        state = RAMP_UP; // Transition to RAMP_UP state after setup
        nextUnitTime = millis() + rampUpDuration;
    }
    else if (state == RAMP_UP && millis() >= nextUnitTime)
    {
        state = RUNNING;
        nextUnitTime = millis() + productionRate;
    }
    else if (state == RUNNING && unitsProduced >= unitsInJob)
    {
        state = SETUP;
        setupTime = millis() + SETUP_DURATION_MEDIAN;
    }
    else if (state == STOPPAGE && millis() >= nextStoppage)
    {
        state = RAMP_UP;
        nextUnitTime = millis() + rampUpDuration;
    }
    else if (state == LUNCH_BREAK && millis() >= nextLunchBreak)
    {
        state = RUNNING;
        nextUnitTime = millis() + productionRate;
    }
}
