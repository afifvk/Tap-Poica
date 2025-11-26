#pragma once

#include <inttypes.h>
#include "BMA250.h"

#define MAX_SAMPLES 16      // Use a power of 2 for faster modulo
#define SAMPLE_MICROS 4166  // 240Hz
#define CUTOFF_FREQ 71      // Hz

#define TWOPI 6.28318530717958647693
#define SAMPLE_INTERVAL (SAMPLE_MICROS / 1000000.0)
#define ALPHA (1 / (TWOPI * SAMPLE_INTERVAL * CUTOFF_FREQ + 1))

// Immutable view into a SampleBuffer
struct SampleBufferView {
  const float *first;
  const size_t first_len;
  const float *second;
  const size_t second_len;
};

// Simple ring buffer of sample magnitudes.
class SampleBuffer {
private:
  size_t mask(size_t index) {
    return index % MAX_SAMPLES;
  }

  size_t mask2(size_t index) {
    return index % (2 * MAX_SAMPLES);
  }

public:
  size_t start = 0, end = 0;
  float buffer[MAX_SAMPLES];
  float total = 0, squares_total = 0;
  float last_mag = NAN;

  SampleBuffer() {}

  size_t len() {
    size_t wrap_offset = (size_t)2 * MAX_SAMPLES * (end < start);
    size_t adjusted_end = end + wrap_offset;
    return adjusted_end - start;
  }

  float get(size_t index) {
    return buffer[mask(start + index)];
  }

  // Appends a new sample, discarding the oldest sample if there is no space.
  void append(Sample sample) {
    float mag = 0;
    mag += (int32_t)sample.x * sample.x;
    mag += (int32_t)sample.y * sample.y;
    mag += (int32_t)sample.z * sample.z;
    mag = sqrt(mag);

    bool is_full = mask2(end + MAX_SAMPLES) == start;
    if (is_full) {
      float val = buffer[mask(start)];
      total -= val;
      squares_total -= val * val;
      start = mask2(start + 1);
    }

    if (isnan(last_mag)) {
      buffer[mask(end)] = mag;
    } else {
      // https://en.wikipedia.org/wiki/High-pass_filter#Discrete-time_realization
      float prev = buffer[mask(end - 1)];
      float unscaled = prev + mag - last_mag;
      buffer[mask(end)] = ALPHA * unscaled;
    }

    float val = buffer[mask(end)];
    total += val;
    squares_total += val * val;
    last_mag = mag;
    end = mask2(end + 1);
  }

  SampleBufferView view() {
    size_t length = len();
    size_t first_start = mask(start);
    size_t first_end = min(MAX_SAMPLES, first_start + length);
    size_t first_len = first_end - first_start;
    return SampleBufferView{
      .first = &buffer[first_start],
      .first_len = first_len,
      .second = &buffer[0],
      .second_len = length - first_len,
    };
  }

  float mean() {
    return total / len();
  }

  float std_dev() {
    //   let a = sum((x - mean) ** 2)
    //         = sum(x ** 2) - sum(2*x*mean) + sum(mean ** 2)
    //         = sum(x ** 2) - 2*mean*sum(x) + total*mean
    // std_dev = sqrt(a / len())
    float a = squares_total - 2 * mean() * total + total * mean();
    return sqrt(a / len());
  }
};
