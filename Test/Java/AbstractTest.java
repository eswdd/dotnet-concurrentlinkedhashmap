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

import java.util.Map;
import java.util.concurrent.ScheduledExecutorService;

import com.googlecode.concurrentlinkedhashmap.ConcurrentLinkedHashMap.Builder;
import org.mockito.ArgumentCaptor;
import org.mockito.Captor;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.testng.ITestResult;
import org.testng.annotations.AfterMethod;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.BeforeSuite;
import org.testng.annotations.DataProvider;
import org.testng.annotations.Optional;
import org.testng.annotations.Parameters;

import static com.googlecode.concurrentlinkedhashmap.IsValidConcurrentLinkedHashMap.valid;
import static com.googlecode.concurrentlinkedhashmap.IsValidLinkedDeque.validLinkedDeque;
import static org.hamcrest.MatcherAssert.assertThat;
import static org.hamcrest.Matchers.is;
import static org.hamcrest.Matchers.nullValue;
import static org.mockito.Mockito.doThrow;
import static org.mockito.MockitoAnnotations.initMocks;

/**
 * A testing harness for simplifying the unit tests.
 *
 * @author ben.manes@gmail.com (Ben Manes)
 */
public abstract class AbstractTest {
  private static boolean debug;
  private long capacity;

  @Mock protected EvictionListener<Integer, Integer> listener;
  @Captor protected ArgumentCaptor<Runnable> catchUpTask;
  @Mock protected ScheduledExecutorService executor;
  @Mock protected Weigher<Integer> weigher;

  /** Retrieves the maximum weighted capacity to build maps with. */
  protected final long capacity() {
    return capacity;
  }

  /* ---------------- Logging methods -------------- */

  protected static void info(String message, Object... args) {
    if (args.length == 0) {
      System.out.println(message);
    } else {
      System.out.printf(message + "\n", args);
    }
  }

  protected static void debug(String message, Object... args) {
    if (debug) {
      info(message, args);
    }
  }

  /* ---------------- Testing aspects -------------- */

  @Parameters("debug")
  @BeforeSuite(alwaysRun = true)
  public static void initSuite(@Optional("false") boolean debugMode) {
    debug = debugMode;
  }

  @Parameters("capacity")
  @BeforeClass(alwaysRun = true)
  public void initClass(long capacity) {
    this.capacity = capacity;
    initMocks(this);
  }

  @AfterMethod(alwaysRun = true)
  public void validateIfSuccessful(ITestResult result) {
    try {
      if (result.isSuccess()) {
        for (Object param : result.getParameters()) {
          validate(param);
        }
      }
    } catch (AssertionError caught) {
      result.setStatus(ITestResult.FAILURE);
      result.setThrowable(caught);
    }
    initMocks(this);
  }

  /** Validates the state of the injected parameter. */
  private static void validate(Object param) {
    if (param instanceof ConcurrentLinkedHashMap<?, ?>) {
      assertThat((ConcurrentLinkedHashMap<?, ?>) param, is(valid()));
    } else if (param instanceof LinkedDeque<?>) {
      assertThat((LinkedDeque<?>) param, is(validLinkedDeque()));
    }
  }

  /* ---------------- Map providers -------------- */


  /* ---------------- Weigher providers -------------- */

  @DataProvider(name = "singletonEntryWeigher")
  public Object[][] providesSingletonEntryWeigher() {
    return new Object[][] {{ Weighers.entrySingleton() }};
  }

  @DataProvider(name = "singletonWeigher")
  public Object[][] providesSingletonWeigher() {
    return new Object[][] {{ Weighers.singleton() }};
  }

  @DataProvider(name = "byteArrayWeigher")
  public Object[][] providesByteArrayWeigher() {
    return new Object[][] {{ Weighers.byteArray() }};
  }

  @DataProvider(name = "iterableWeigher")
  public Object[][] providesIterableWeigher() {
    return new Object[][] {{ Weighers.iterable() }};
  }

  @DataProvider(name = "collectionWeigher")
  public Object[][] providesCollectionWeigher() {
    return new Object[][] {{ Weighers.collection() }};
  }

  @DataProvider(name = "listWeigher")
  public Object[][] providesListWeigher() {
    return new Object[][] {{ Weighers.list() }};
  }

  @DataProvider(name = "setWeigher")
  public Object[][] providesSetWeigher() {
    return new Object[][] {{ Weighers.set() }};
  }

  @DataProvider(name = "mapWeigher")
  public Object[][] providesMapWeigher() {
    return new Object[][] {{ Weighers.map() }};
  }

}
