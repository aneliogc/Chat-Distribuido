import React from 'react';
import { View, ActivityIndicator, StyleSheet } from 'react-native';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { StatusBar } from 'expo-status-bar';

import { AuthProvider, useAuth } from './src/context/AuthContext';
import { RealtimeProvider } from './src/realtime/RealtimeContext';
import LoginScreen from './src/screens/LoginScreen';
import RegisterScreen from './src/screens/RegisterScreen';
import ConversationsScreen from './src/screens/ConversationsScreen';
import ChatScreen from './src/screens/ChatScreen';
import NewConversationScreen from './src/screens/NewConversationScreen';

const Stack = createNativeStackNavigator();

// Decide QUAIS telas existem com base no estado de login.
function RootNavigator() {
  const { session, loading } = useAuth();

  // Enquanto recupera a sessao salva (boot do app), mostra um spinner.
  if (loading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color="#2563eb" />
      </View>
    );
  }

  return (
    <Stack.Navigator
      screenOptions={{
      headerTransparent: true,
      headerTitle: '',
      headerBackTitleVisible: false,
      headerTintColor: '#6366f1',
    }}>
      {session ? (
        // Logado: telas do app.
        <>
          <Stack.Screen
            name="Conversations"
            component={ConversationsScreen}
            options={{ title: 'Conversas' }}
          />
          {/* Chat e Nova conversa tem cabecalho proprio -> esconde o nativo. */}
          <Stack.Screen
            name="Chat"
            component={ChatScreen}
            options={{ headerShown: false }}
          />
          <Stack.Screen
            name="NewConversation"
            component={NewConversationScreen}
            options={{ headerShown: false }}
          />
        </>
      ) : (
        // Deslogado: autenticacao.
        <>
          <Stack.Screen name="Login" component={LoginScreen} options={{ title: 'Entrar' }} />
          <Stack.Screen name="Register" component={RegisterScreen} options={{ title: 'Criar conta' }} />
        </>
      )}
    </Stack.Navigator>
  );
}

export default function App() {
  return (
    <SafeAreaProvider>
      <AuthProvider>
        <RealtimeProvider>
          <NavigationContainer>
            <RootNavigator />
          </NavigationContainer>
        </RealtimeProvider>
        <StatusBar style="auto" />
      </AuthProvider>
    </SafeAreaProvider>
  );
}

const styles = StyleSheet.create({
  center: { flex: 1, justifyContent: 'center', alignItems: 'center' },
});
