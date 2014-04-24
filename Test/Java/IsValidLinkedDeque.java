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

import java.util.Deque;
import java.util.Iterator;
import java.util.Set;

import com.google.common.collect.Sets;
import org.hamcrest.Description;
import org.hamcrest.Factory;
import org.hamcrest.TypeSafeDiagnosingMatcher;

import static com.googlecode.concurrentlinkedhashmap.IsEmptyCollection.emptyCollection;
import static org.hamcrest.Matchers.hasSize;
import static org.hamcrest.Matchers.is;
import static org.hamcrest.Matchers.not;
import static org.hamcrest.Matchers.nullValue;

/**
 * A matcher that evaluates a {@link LinkedDeque} to determine if it is in a
 * valid state.
 *
 * @author ben.manes@gmail.com (Ben Manes)
 */
public final class IsValidLinkedDeque<E>
    extends TypeSafeDiagnosingMatcher<LinkedDeque<? extends E>> {

  @Override
  public void describeTo(Description description) {
    description.appendText("valid");
  }

  @Override
  protected boolean matchesSafely(LinkedDeque<? extends E> deque, Description description) {
    DescriptionBuilder builder = new DescriptionBuilder(description);

    if (deque.isEmpty()) {
      checkEmpty(deque, builder);
    }
    checkIterator(deque, deque.iterator(), builder);
    checkIterator(deque, deque.descendingIterator(), builder);

    return builder.matches();
  }

  void checkEmpty(Deque<? extends Linked<? extends E>> deque, DescriptionBuilder builder) {
    builder.expectThat(deque, emptyCollection());
    builder.expectThat(deque.pollFirst(), is(nullValue()));
    builder.expectThat(deque.pollLast(), is(nullValue()));
    builder.expectThat(deque.poll(), is(nullValue()));
  }

  void checkIterator(Deque<? extends Linked<? extends E>> deque,
      Iterator<? extends Linked<? extends E>> iterator, DescriptionBuilder builder) {
    Set<Linked<?>> seen = Sets.newIdentityHashSet();
    while (iterator.hasNext()) {
      Linked<?> element = iterator.next();
      checkElement(deque, element, builder);
      String errorMsg = String.format("Loop detected: %s in %s", element, seen);
      builder.expectThat(errorMsg, seen.add(element), is(true));
    }
    builder.expectThat(deque, hasSize(seen.size()));
  }

  void checkElement(Deque<? extends Linked<? extends E>> deque, Linked<?> element,
      DescriptionBuilder builder) {
    Linked<?> first = deque.peekFirst();
    Linked<?> last = deque.peekLast();
    if (element == first) {
      builder.expectThat("not null prev", element.getPrevious(), is(nullValue()));
    }
    if (element == last) {
      builder.expectThat("not null next", element.getNext(), is(nullValue()));
    }
    if ((element != first) && (element != last)) {
      builder.expectThat(element.getPrevious(), is(not(nullValue())));
      builder.expectThat(element.getNext(), is(not(nullValue())));
    }
  }

  @Factory
  public static <E> IsValidLinkedDeque<E> validLinkedDeque() {
    return new IsValidLinkedDeque<E>();
  }
}
