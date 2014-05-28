/*
 * Copyright 2011 Google Inc. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
package com.googlecode.concurrentlinkedhashmap;

import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.concurrent.Callable;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.locks.ReentrantLock;

import com.google.common.collect.ImmutableMap;
import com.google.common.collect.Lists;
import com.googlecode.concurrentlinkedhashmap.ConcurrentLinkedHashMap.Builder;
import com.googlecode.concurrentlinkedhashmap.ConcurrentLinkedHashMap.Node;
import com.googlecode.concurrentlinkedhashmap.ConcurrentLinkedHashMap.PaddedAtomicLong;
import com.googlecode.concurrentlinkedhashmap.ConcurrentLinkedHashMap.PaddedAtomicReference;
import com.googlecode.concurrentlinkedhashmap.benchmark.Benchmarks.EfficiencyRun;
import com.googlecode.concurrentlinkedhashmap.caches.CacheFactory;
import com.googlecode.concurrentlinkedhashmap.generator.Generator;
import com.googlecode.concurrentlinkedhashmap.generator.ScrambledZipfianGenerator;
import org.mockito.Mockito;
import org.testng.annotations.Test;

import static com.google.common.collect.Maps.newLinkedHashMap;
import static com.google.common.collect.Sets.newLinkedHashSet;
import static com.googlecode.concurrentlinkedhashmap.ConcurrentLinkedHashMap.MAXIMUM_CAPACITY;
import static com.googlecode.concurrentlinkedhashmap.ConcurrentLinkedHashMap.READ_BUFFER_THRESHOLD;
import static com.googlecode.concurrentlinkedhashmap.ConcurrentLinkedHashMap.readBufferIndex;
import static com.googlecode.concurrentlinkedhashmap.ConcurrentLinkedHashMap.DrainStatus.REQUIRED;
import static com.googlecode.concurrentlinkedhashmap.EvictionTest.Status.ALIVE;
import static com.googlecode.concurrentlinkedhashmap.EvictionTest.Status.DEAD;
import static com.googlecode.concurrentlinkedhashmap.EvictionTest.Status.RETIRED;
import static com.googlecode.concurrentlinkedhashmap.IsEmptyCollection.emptyCollection;
import static com.googlecode.concurrentlinkedhashmap.IsEmptyMap.emptyMap;
import static com.googlecode.concurrentlinkedhashmap.IsValidConcurrentLinkedHashMap.valid;
import static com.googlecode.concurrentlinkedhashmap.benchmark.Benchmarks.createWorkingSet;
import static com.googlecode.concurrentlinkedhashmap.benchmark.Benchmarks.determineEfficiency;
import static com.jayway.awaitility.Awaitility.await;
import static com.jayway.awaitility.Awaitility.to;
import static java.util.Arrays.asList;
import static java.util.Collections.singletonMap;
import static org.hamcrest.MatcherAssert.assertThat;
import static org.hamcrest.Matchers.containsInAnyOrder;
import static org.hamcrest.Matchers.equalTo;
import static org.hamcrest.Matchers.hasSize;
import static org.hamcrest.Matchers.is;
import static org.hamcrest.Matchers.not;
import static org.hamcrest.Matchers.nullValue;
import static org.mockito.Matchers.anyInt;
import static org.mockito.Mockito.doThrow;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;

/**
 * A unit-test for the page replacement algorithm and its public methods.
 *
 * @author ben.manes@gmail.com (Ben Manes)
 */
@Test(groups = "development")
public final class EvictionTest extends AbstractTest {



  /* ---------------- Ascending KeySet -------------- */

  @Test(dataProvider = "warmedMap")
  public void ascendingKeySet(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    warmUp(expected, 0, capacity());

    assertThat(map.ascendingKeySet(), is(equalTo(expected.keySet())));
  }

  @Test(dataProvider = "warmedMap")
  public void ascendingKeySet_snapshot(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    warmUp(expected, 0, capacity());

    Set<Integer> original = map.ascendingKeySet();
    map.put((int) capacity(), (int) -capacity());

    assertThat(original, is(equalTo(expected.keySet())));
  }

  @Test(dataProvider = "warmedMap")
  public void ascendingKeySetWithLimit_greaterThan(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    warmUp(expected, 0, capacity() / 2);

    assertThat(map.ascendingKeySetWithLimit((int) capacity() / 2), is(equalTo(expected.keySet())));
  }

  @Test(dataProvider = "warmedMap")
  public void ascendingKeySetWithLimit_lessThan(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    warmUp(expected, 0, capacity());

    assertThat(map.ascendingKeySetWithLimit((int) capacity() * 2), is(equalTo(expected.keySet())));
  }

  @Test(dataProvider = "warmedMap")
  public void ascendingKeySetWithLimit_snapshot(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    warmUp(expected, 0, capacity() / 2);

    Set<Integer> original = map.ascendingKeySetWithLimit((int) capacity() / 2);
    map.put((int) capacity(), (int) -capacity());

    assertThat(original, is(equalTo(expected.keySet())));
  }

  @Test(dataProvider = "warmedMap")
  public void ascendingKeySetWithLimit_zero(ConcurrentLinkedHashMap<Integer, Integer> map) {
    assertThat(map.ascendingKeySetWithLimit(0), is(emptyCollection()));
  }

  @Test(dataProvider = "warmedMap", expectedExceptions = IllegalArgumentException.class)
  public void ascendingKeySetWithLimit_negative(ConcurrentLinkedHashMap<Integer, Integer> map) {
    map.ascendingKeySetWithLimit(-1);
  }

  /* ---------------- Descending KeySet -------------- */

  @Test(dataProvider = "warmedMap")
  public void descendingKeySet(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Set<Integer> expected = newLinkedHashSet();
    for (int i = (int) capacity() - 1; i >= 0; i--) {
      expected.add(i);
    }

    assertThat(map.descendingKeySet(), is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void descendingKeySet_snapshot(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Set<Integer> expected = newLinkedHashSet();
    for (int i = (int) capacity() - 1; i >= 0; i--) {
      expected.add(i);
    }

    Set<Integer> original = map.descendingKeySet();
    map.put((int) capacity(), (int) -capacity());

    assertThat(original, is(equalTo(original)));
  }

  @Test(dataProvider = "warmedMap")
  public void descendingKeySetWithLimit_greaterThan(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Set<Integer> expected = newLinkedHashSet();
    for (int i = (int) capacity() - 1; i >= capacity() / 2; i--) {
      expected.add(i);
    }
    assertThat(map.descendingKeySetWithLimit((int) capacity() / 2), is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void descendingKeySetWithLimit_lessThan(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Set<Integer> expected = newLinkedHashSet();
    for (int i = (int) capacity() - 1; i >= 0; i--) {
      expected.add(i);
    }
    assertThat(map.descendingKeySetWithLimit((int) capacity() * 2), is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void descendingKeySetWithLimit_snapshot(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Set<Integer> expected = newLinkedHashSet();
    for (int i = (int) capacity() - 1; i >= capacity() / 2; i--) {
      expected.add(i);
    }

    Set<Integer> original = map.descendingKeySetWithLimit((int) capacity() / 2);
    map.put((int) capacity(), (int) -capacity());

    assertThat(original, is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void descendingKeySetWithLimit_zero(ConcurrentLinkedHashMap<Integer, Integer> map) {
    assertThat(map.descendingKeySetWithLimit(0), is(emptyCollection()));
  }

  @Test(dataProvider = "warmedMap", expectedExceptions = IllegalArgumentException.class)
  public void descendingKeySetWithLimit_negative(ConcurrentLinkedHashMap<Integer, Integer> map) {
    map.descendingKeySetWithLimit(-1);
  }

  /* ---------------- Ascending Map -------------- */

  @Test(dataProvider = "warmedMap")
  public void ascendingMap(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    warmUp(expected, 0, capacity());

    assertThat(map.ascendingMap(), is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void ascendingMap_snapshot(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    warmUp(expected, 0, capacity());

    Map<Integer, Integer> original = map.ascendingMap();
    map.put((int) capacity(), (int) -capacity());

    assertThat(original, is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void ascendingMapWithLimit_greaterThan(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    warmUp(expected, 0, capacity() / 2);

    assertThat(map.ascendingMapWithLimit((int) capacity() / 2), is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void ascendingMapWithLimit_lessThan(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    warmUp(expected, 0, capacity());

    assertThat(map.ascendingMapWithLimit((int) capacity() * 2), is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void ascendingMapWithLimit_snapshot(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    warmUp(expected, 0, capacity() / 2);

    Map<Integer, Integer> original = map.ascendingMapWithLimit((int) capacity() / 2);
    map.put((int) capacity(), (int) -capacity());

    assertThat(original, is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void ascendingMapWithLimit_zero(ConcurrentLinkedHashMap<Integer, Integer> map) {
    assertThat(map.ascendingMapWithLimit(0), is(emptyMap()));
  }

  @Test(dataProvider = "warmedMap", expectedExceptions = IllegalArgumentException.class)
  public void ascendingMapWithLimit_negative(ConcurrentLinkedHashMap<Integer, Integer> map) {
    map.ascendingMapWithLimit(-1);
  }

  /* ---------------- Descending Map -------------- */

  @Test(dataProvider = "warmedMap")
  public void descendingMap(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    for (int i = (int) capacity() - 1; i >= 0; i--) {
      expected.put(i, -i);
    }
    assertThat(map.descendingMap(), is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void descendingMap_snapshot(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    for (int i = (int) capacity() - 1; i >= 0; i--) {
      expected.put(i, -i);
    }

    Map<Integer, Integer> original = map.descendingMap();
    map.put((int) capacity(), (int) -capacity());

    assertThat(original, is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void descendingMapWithLimit_greaterThan(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    for (int i = (int) capacity() - 1; i >= capacity() / 2; i--) {
      expected.put(i, -i);
    }
    assertThat(map.descendingMapWithLimit((int) capacity() / 2), is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void descendingMapWithLimit_lessThan(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    for (int i = (int) capacity() - 1; i >= 0; i--) {
      expected.put(i, -i);
    }
    assertThat(map.descendingMapWithLimit((int) capacity() * 2), is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void descendingMapWithLimit_snapshot(ConcurrentLinkedHashMap<Integer, Integer> map) {
    Map<Integer, Integer> expected = newLinkedHashMap();
    for (int i = (int) capacity() - 1; i >= capacity() / 2; i--) {
      expected.put(i, -i);
    }

    Map<Integer, Integer> original = map.descendingMapWithLimit((int) capacity() / 2);
    map.put((int) capacity(), (int) -capacity());

    assertThat(original, is(equalTo(expected)));
  }

  @Test(dataProvider = "warmedMap")
  public void descendingMapWithLimit_zero(ConcurrentLinkedHashMap<Integer, Integer> map) {
    assertThat(map.descendingMapWithLimit(0), is(emptyMap()));
  }

  @Test(dataProvider = "warmedMap", expectedExceptions = IllegalArgumentException.class)
  public void descendingMapWithLimit_negative(ConcurrentLinkedHashMap<Integer, Integer> map) {
    map.descendingMapWithLimit(-1);
  }
}
