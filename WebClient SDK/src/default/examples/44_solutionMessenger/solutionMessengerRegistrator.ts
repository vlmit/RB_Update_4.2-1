import { MessengersRegistry } from 'tessa/ui/messengers';
import { FriendsMessenger } from './friendsMessenger';

export function registerSolutionMessenger(): void {
  MessengersRegistry.instance.register(FriendsMessenger.code, new FriendsMessenger());
}
